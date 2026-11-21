using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenCatchPreparationChecks
{
    internal const string TestName = "compiler/beam/shortens-catch-after-proven-append-preparation";
    private const string Module = "beam_proven_catch_append";
    private const int UnknownAppendLine = 13;
    private const int SinkErrorLine = 16;

    private const string Source = """
        -module(beam_proven_catch_append).
        -export([wrapped/2, uncertain/2]).

        wrapped(first, Tail) ->
            proven("one", Tail);
        wrapped(second, Tail) ->
            proven("two", Tail).

        proven(Format, Tail) ->
            catch consume("outer preparation: " ++ "nested preparation: " ++ Format ++ "!", [prefix, self() | Tail]).

        uncertain(Format, Tail) ->
            catch consume("outer preparation: " ++ "nested preparation: " ++ Format ++ "!", [prefix, self() | Tail]).

        consume(_, [prefix, _ | fail]) ->
            erlang:error(sink_failure);
        consume(Text, [prefix, Pid | Payload]) ->
            {Text, is_pid(Pid), Payload}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        VerifyBoundaries(Code(beam));

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tail = Term.List(Term.A("payload"));
            Term first = await image.Invoke(
                context,
                "wrapped",
                Term.A("first"),
                tail
            );
            Term second = await image.Invoke(
                context,
                "wrapped",
                Term.A("second"),
                tail
            );
            if (!Term.Tuple(Term.String("outer preparation: nested preparation: one!"), Term.A("true"), tail).Equals(first)
                || !Term.Tuple(Term.String("outer preparation: nested preparation: two!"), Term.A("true"), tail).Equals(second))
            {
                throw new InvalidOperationException(BeamProvenCatchPreparationDiagnostics.PreparedValues);
            }

            Term unknown = await image.Invoke(
                context,
                "uncertain",
                Term.I(7),
                tail
            );
            if (!HasCaughtFrame(
                unknown,
                "badarg",
                "uncertain",
                UnknownAppendLine
            ))
            {
                throw new InvalidOperationException(BeamProvenCatchPreparationDiagnostics.UnknownOperand);
            }

            Term protectedEffect = await image.Invoke(
                context,
                "wrapped",
                Term.A("first"),
                Term.A("fail")
            );
            if (!HasCaughtFrame(
                protectedEffect,
                "sink_failure",
                "consume",
                SinkErrorLine
            ))
            {
                throw new InvalidOperationException(BeamProvenCatchPreparationDiagnostics.ProtectedEffect);
            }

            return Nil.Value;
        });
    }

    private static bool HasCaughtFrame(
        Term result,
        string expectedReason,
        string expectedFunction,
        int expectedLine
    )
    {
        if (result is not TupleTerm
            {
                Items: [Atom { Name: "EXIT" }, TupleTerm { Items: [Atom reason, var stack] }]
            } || reason.Name != expectedReason)
        {
            return false;
        }

        Term line = Term.Tuple(Term.A("line"), Term.I(expectedLine));

        return Cons.Items(stack).Any(frame =>
            frame is TupleTerm { Items: [Atom module, Atom function, _, var location] }
            && module.Name == Module && function.Name == expectedFunction && Cons.Items(location).Contains(line));
    }

    private static void VerifyBoundaries(object code)
    {
        object[] proven = FunctionBody(code, "proven");
        int provenCatch = Array.FindIndex(proven, instruction => Opcode(instruction) == CatchEnter);
        int lastAppend = Array.FindLastIndex(proven, instruction => Opcode(instruction) == ExternalCall);
        int allocation = Array.FindIndex(proven, instruction => Opcode(instruction) == Allocate);
        if (allocation < 0 || lastAppend <= allocation || provenCatch <= lastAppend
            || !Operands(proven[allocation]).Select(Value).SequenceEqual([1, 2])
            || !Operands(proven[provenCatch]).Take(1)
                .Any(operand => Property(operand, "Kind") is "y" && Value(operand) == 0))
        {
            throw new InvalidOperationException(BeamProvenCatchPreparationDiagnostics.ProvenBoundary);
        }

        object[] uncertain = FunctionBody(code, "uncertain");
        int uncertainCatch = Array.FindIndex(uncertain, instruction => Opcode(instruction) == CatchEnter);
        int firstAppend = Array.FindIndex(uncertain, instruction => Opcode(instruction) == ExternalCall);
        if (uncertainCatch < 0 || firstAppend <= uncertainCatch)
        {
            throw new InvalidOperationException(BeamProvenCatchPreparationDiagnostics.UnknownOperand);
        }
    }

    private static object[] FunctionBody(object code, string function)
    {
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == function
        );

        return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
