using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCallConstructorChecks
{
    private const string Module = "beam_call_constructor";
    private const string Source = """
        -module(beam_call_constructor).
        -export([init/1, init_with_prefix/1, repeated/1, improper/1, effectful/1]).

        init([]) ->
            Flags = {ordered, 2, 7},
            Values = lists:map(fun make/1, [first, second]),
            {ok, {Flags, Values}}.

        init_with_prefix([]) ->
            seed(first),
            Flags = {ordered, 2, 7},
            observe_second(),
            Values = lists:map(fun make/1, [first, second]),
            {ok, {Flags, Values}}.

        seed(Event) ->
            put(events, [Event]).

        observe_second() ->
            Seen = get(events),
            put(events, [second | Seen]).

        make(Value) ->
            {Value, [Value], {wrapped, Value}}.

        repeated(Value) ->
            {Value, {wrapped, Value}, [Value, Value, suffix]}.

        improper(Value) ->
            {Value, {wrapped, Value}, [first, Value, second | Value]}.

        effectful(Value) ->
            {Value, {wrapped, Value}, [remember(Value), Value, suffix]}.

        remember(Value) ->
            erlang:put(last_input, Value),
            recorded.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] operations = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int allocation = Array.IndexOf(operations, BeamEmissionFormat.AllocateHeap);
        ReadOnlySpan<byte> siblings =
        [
            BeamEmissionFormat.TestHeap, BeamEmissionFormat.PutList,
            BeamEmissionFormat.PutTuple, BeamEmissionFormat.PutTuple, BeamEmissionFormat.Return
        ];
        if (allocation < 0 || operations[allocation + 1] != BeamEmissionFormat.MakeFun
            || OperandValue(instructions[allocation], 0) != 0
            || operations.AsSpan().IndexOf(siblings) < 0)
        {
            throw new InvalidOperationException(BeamCallConstructorDiagnostics.Instructions);
        }

        ReadOnlySpan<byte> prefix =
        [
            BeamEmissionFormat.Allocate, BeamEmissionFormat.Move, BeamEmissionFormat.LocalCall,
            BeamEmissionFormat.LocalCall, BeamEmissionFormat.TestHeap, BeamEmissionFormat.MakeFun
        ];
        byte[] withoutLines = operations.Where(opcode => opcode != BeamEmissionFormat.Line).ToArray();
        int prefixFrame = Array.IndexOf(operations, BeamEmissionFormat.Allocate);
        if (withoutLines.AsSpan().IndexOf(prefix) < 0 || prefixFrame < 0 || OperandValue(instructions[prefixFrame], 0) != 0)
        {
            throw new InvalidOperationException(BeamCallConstructorDiagnostics.PrefixFrame);
        }

        ReadOnlySpan<byte> compound =
        [
            BeamEmissionFormat.TestHeap, BeamEmissionFormat.PutTuple, BeamEmissionFormat.PutList,
            BeamEmissionFormat.PutList, BeamEmissionFormat.PutTuple, BeamEmissionFormat.Return
        ];
        int construction = operations.AsSpan().IndexOf(compound);
        if (construction < 0 || OperandValue(instructions[construction], 0) != 11
            || OperandValue(instructions[construction + 2], 2) != 2
            || OperandValue(instructions[construction + 3], 1) != 2
            || OperandValue(instructions[construction + 3], 2) != 2)
        {
            throw new InvalidOperationException(BeamCallConstructorDiagnostics.SiblingListRegisters);
        }

        await using var runtime = new ProcessRuntime();
        var dependencies = new BeamDependencySelection(["lists", ErlangModuleNames.Module]);
        BeamModule image = BeamModule.Prepare(beam, dependencies);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term expected = Term.Tuple(
                Term.A("ok"),
                Term.Tuple(
                Term.Tuple(Term.A("ordered"), Term.I(2), Term.I(7)),
                Term.List(Item(Term.A("first")), Item(Term.A("second")))
            )
            );
            if (!expected.Equals(await image.Invoke(context, "init", Nil.Value)))
            {
                throw new InvalidOperationException(BeamCallConstructorDiagnostics.Result);
            }

            Term events = Term.A("events");
            try
            {
                await image.Invoke(context, "init_with_prefix", Term.A("wrong"));
                throw new InvalidOperationException(BeamCallConstructorDiagnostics.InvalidInputAccepted);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                Term observed = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Get,
                    events
                );
                if (!observed.Equals(Term.A("undefined")))
                {
                    throw new InvalidOperationException(BeamCallConstructorDiagnostics.PrefixEffects);
                }
            }

            if (!expected.Equals(await image.Invoke(context, "init_with_prefix", Nil.Value)))
            {
                throw new InvalidOperationException(BeamCallConstructorDiagnostics.Result);
            }

            Term executed = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Get,
                events
            );
            if (!executed.Equals(Term.List(Term.A("second"), Term.A("first"))))
            {
                throw new InvalidOperationException(BeamCallConstructorDiagnostics.PrefixEffects);
            }

            Term value = Term.A("input");
            Term wrapped = Term.Tuple(Term.A("wrapped"), value);
            Term suffix = Term.A("suffix");
            Term tail = new Cons(Term.A("first"), new Cons(value, new Cons(Term.A("second"), value)));
            Term recorded = await image.Invoke(context, "effectful", value);
            Term lastInput = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Get,
                Term.A("last_input")
            );
            if (!(await image.Invoke(context, "repeated", value)).Equals(Term.Tuple(value, wrapped, Term.List(value, value, suffix)))
                || !(await image.Invoke(context, "improper", value)).Equals(Term.Tuple(value, wrapped, tail))
                || !recorded.Equals(Term.Tuple(value, wrapped, Term.List(Term.A("recorded"), value, suffix)))
                || !lastInput.Equals(value))
            {
                throw new InvalidOperationException(BeamCallConstructorDiagnostics.SiblingListResult);
            }

            return Nil.Value;
        });

        static Term Item(Term value) => Term.Tuple(value, Term.List(value), Term.Tuple(Term.A("wrapped"), value));
    }

    private static BigInteger OperandValue(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
