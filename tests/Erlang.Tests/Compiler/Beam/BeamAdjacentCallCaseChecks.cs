using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAdjacentCallCaseChecks
{
    internal const string TestName = "compiler/beam/adjacent-call-keeps-result-live";
    private const string Module = "beam_adjacent_call_case";
    private const string AllocationMismatch = "Expected the first call result in X0 during construction and the saved slots cleared before the next call.";
    private const string GuardAliasMismatch = "A guard on a fresh tuple field must preserve the returned selector in X0.";
    private const string ResultMismatch = "The adjacent call or its case arm received an incorrect argument.";

    private const string Source = """
        -module(beam_adjacent_call_case).
        -export([run/2, retained/2, literal/2, wrapped/2]).
        run(Type, Value) ->
            ok,
            Result = produce(Value, marker),
            case consume(target, Result, [Type, Value]) of
                {ok, ValueInResult} = Success when is_integer(ValueInResult) ->
                    Success;
                {error, {Reason, _}} ->
                    {error, Reason}
            end.
        retained(Type, Value) ->
            Result = produce(Value, marker),
            case consume(target, Result, [Type, Value]) of
                {ok, _} = Success -> {Success, Type};
                {error, {Reason, _}} -> {error, Reason}
            end.
        literal(Type, Value) ->
            ok,
            Result = produce(Value, marker),
            case consume(target, Result, [Type, Value]) of
                {ok, Field} = _Whole when is_integer(Field) -> matched;
                {error, {Reason, _}} -> {error, Reason}
            end.
        wrapped(Type, Value) ->
            ok,
            Result = produce(Value, marker),
            case consume(target, Result, [Type, Value]) of
                {ok, Field} = Whole when is_integer(Field) -> {wrapped, Whole};
                {error, {Reason, _}} -> {error, Reason}
            end.
        produce(Value, marker) -> {prepared, Value}.
        consume(target, {prepared, Value}, [keep, Value]) -> {ok, Value};
        consume(target, {prepared, Value}, [reject, Value]) -> {error, {Value, marker}}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>()
            .Where(instruction => Opcode(instruction) != BeamEmissionFormat.Line).ToArray();
        byte[] operations = instructions.Select(Opcode).ToArray();
        ReadOnlySpan<byte> prefix =
        [
            BeamEmissionFormat.LocalCall,
            BeamEmissionFormat.TestHeap,
            BeamEmissionFormat.PutList,
            BeamEmissionFormat.PutList,
            BeamEmissionFormat.Move,
            BeamEmissionFormat.InitYRegisters,
            BeamEmissionFormat.Move,
            BeamEmissionFormat.LocalCall
        ];
        int start = operations.AsSpan().IndexOf(prefix);
        int allocate = Array.IndexOf(operations, BeamEmissionFormat.Allocate);
        if (start < 0 || allocate < 0 || allocate >= start || Operand(instructions[allocate], 0) != 2
            || Operand(instructions[start + 1], 0) != 4 || Operand(instructions[start + 1], 1) != 1
            || Operand(instructions[start + 2], 2) != 1 || Operand(instructions[start + 3], 2) != 2)
        {
            throw new InvalidOperationException(AllocationMismatch);
        }

        ReadOnlySpan<byte> guardAlias =
        [
            BeamEmissionFormat.GetTupleElement,
            BeamEmissionFormat.IsInteger,
            BeamEmissionFormat.Deallocate,
            BeamEmissionFormat.Return
        ];
        int alias = operations.AsSpan().IndexOf(guardAlias);
        if (alias < 0 || Operand(instructions[alias], 0) != 0 || Operand(instructions[alias], 1) != 1
            || Operand(instructions[alias], 2) != 2 || Operand(instructions[alias + 1], 1) != 2)
        {
            throw new InvalidOperationException(GuardAliasMismatch);
        }

        BeamModule image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term keep = Term.A("keep");
            Term reject = Term.A("reject");
            Term value = Term.I(7);
            Term success = Term.Tuple(Term.A("ok"), value);
            if (!success.Equals(await image.Invoke(
                context,
                "run",
                keep,
                value
            ))
                || !Term.Tuple(Term.A("error"), value).Equals(await image.Invoke(
                    context,
                    "run",
                    reject,
                    value
                ))
                || !Term.Tuple(success, keep).Equals(await image.Invoke(
                    context,
                    "retained",
                    keep,
                    value
                ))
                || !Term.A("matched").Equals(await image.Invoke(
                    context,
                    "literal",
                    keep,
                    value
                ))
                || !Term.Tuple(Term.A("wrapped"), success).Equals(await image.Invoke(
                    context,
                    "wrapped",
                    keep,
                    value
                )))
            {
                throw new InvalidOperationException(ResultMismatch);
            }

            Term invalid = Term.A("invalid");
            try
            {
                await image.Invoke(
                    context,
                    "run",
                    keep,
                    invalid
                );
                throw new InvalidOperationException(GuardAliasMismatch);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.Tuple(
                Term.A("case_clause"),
                Term.Tuple(Term.A("ok"), invalid)
            )))
            {
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
