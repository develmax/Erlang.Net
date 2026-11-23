using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSelectorArgumentConstructionChecks
{
    internal const string TestName = "compiler/beam/selector-builds-in-outgoing-argument";
    private const string Module = "beam_selector_construction";
    private const string ConstructionMismatch = "The selector must build its argument in the outgoing register with no saved entry arguments.";
    private const string ResultMismatch = "The selector call must receive the retained and constructed arguments and keep later uses live.";
    private const string InvalidCase = "The selector result must raise case_clause when no arm matches.";

    private const string Source = """
        -module(beam_selector_construction).
        -export([forward/2,reversed/2,retained/2]).
        forward(Key, Data) ->
            case select(Key, {packet, [Data]}) of
                {ok, Value} -> Value;
                {error, Reason} -> Reason
            end.
        reversed(Data, Key) ->
            case select_reversed({packet, [Data]}, Key) of
                {ok, Value} -> Value;
                {error, Reason} -> Reason
            end.
        retained(Key, Data) ->
            case select(Key, {packet, [Data]}) of
                {ok, Value} -> {Value, Data};
                {error, Reason} -> Reason
            end.
        select(Key, Value) -> {Key, Value}.
        select_reversed(Value, Key) -> {Key, Value}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>()
            .Where(instruction => Opcode(instruction) != BeamEmissionFormat.Line).ToArray();
        byte[] operations = instructions.Select(Opcode).ToArray();
        ReadOnlySpan<byte> prefix =
        [
            BeamEmissionFormat.AllocateHeap,
            BeamEmissionFormat.PutList,
            BeamEmissionFormat.PutTuple,
            BeamEmissionFormat.LocalCall
        ];
        int forward = operations.AsSpan().IndexOf(prefix);
        int reverseOffset = forward < 0 ? -1 : operations.AsSpan(forward + prefix.Length).IndexOf(prefix);
        if (reverseOffset < 0)
        {
            throw new InvalidOperationException(ConstructionMismatch);
        }

        CheckConstruction(forward, register: 1);
        CheckConstruction(forward + prefix.Length + reverseOffset, register: 0);

        BeamModule image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.Tuple(Term.A("input"), Term.I(1));
            Term expected = Term.Tuple(Term.A("packet"), Term.List(input));
            Term ok = Term.A("ok");
            Term error = Term.A("error");
            if (!expected.Equals(await image.Invoke(
                context,
                "forward",
                ok,
                input
            ))
                || !expected.Equals(await image.Invoke(
                    context,
                    "reversed",
                    input,
                    error
                ))
                || !Term.Tuple(expected, input).Equals(await image.Invoke(
                    context,
                    "retained",
                    ok,
                    input
                )))
            {
                throw new InvalidOperationException(ResultMismatch);
            }

            Term unexpected = Term.A("unexpected");
            try
            {
                await image.Invoke(
                    context,
                    "forward",
                    unexpected,
                    input
                );
                throw new InvalidOperationException(InvalidCase);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.Tuple(
                Term.A("case_clause"),
                Term.Tuple(unexpected, expected)
            )))
            {
            }

            return Nil.Value;
        });

        void CheckConstruction(int start, int register)
        {
            if (Operand(instructions[start], 0) != 0 || Operand(instructions[start], 1) != 5
                || Operand(instructions[start], 2) != 2 || Operand(instructions[start + 1], 0) != register
                || Operand(instructions[start + 1], 2) != register || Operand(instructions[start + 2], 0) != register)
            {
                throw new InvalidOperationException(ConstructionMismatch);
            }
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
