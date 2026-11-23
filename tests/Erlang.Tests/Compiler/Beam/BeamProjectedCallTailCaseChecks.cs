using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedCallTailCaseChecks
{
    internal const string TestName = "compiler/beam/preserves-projection-order-and-shared-caught-call-tails";
    private const string Fixture = "beam_projected_call_tail_case_probe.erl";
    private const string UnexpectedLayout = "Projected tuple call tails did not keep the pinned continuation.";

    internal const string Source = """
        -module(beam_projected_call_tail_case_probe).
        -export([process/1, scan/2, inspect/1, inspect_binary/1]).

        process(Mode) ->
            Saved = prepare(Mode, []),
            case catch ?MODULE:inspect(Saved) of
                {ok, Value} -> {ok, Value};
                {error, Reason} -> report(Reason, Saved);
                {'EXIT', Reason} -> report(Reason, Saved)
            end.

        scan(_, Data) when is_binary(Data) ->
            case
                ?MODULE:inspect_binary(Data)
            of
                {ok, Value, _, _} -> pass(Value);
                {error, Reason, Location} -> report(Reason, Location, [])
            end;
        scan(_, _) ->
            {error, not_binary}.

        prepare(Mode, []) ->
            self() ! {prepared, Mode},
            Mode.

        inspect(ok) -> {ok, processed};
        inspect(error) -> {error, private_error};
        inspect(crash) -> erlang:error(private_failure);
        inspect(other) -> unrelated.

        inspect_binary(Data) ->
            self() ! {read, Data},
            case Data of
                <<1>> -> {ok, accepted, marker, location};
                <<2>> -> {error, malformed, second};
                _ -> unrelated
            end.

        report(Reason, Saved) -> {reported, Reason, Saved}.
        report(Reason, Location, Suffix) -> {reported, Reason, Location, Suffix}.
        pass(Value) -> {accepted, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var process = Body("process", 1);
        var scanner = Body("scan", 2);
        int catchIndex = Array.FindIndex(process, instruction => Opcode(instruction) == CatchEnter);
        int returnIndex = Array.FindIndex(scanner, instruction => Opcode(instruction) == Return);
        int caseEndIndex = Array.FindIndex(scanner, instruction => Opcode(instruction) == CaseEnd);
        if (catchIndex < 0 || Opcode(process[catchIndex + 1]) == Move
            || process.Count(instruction => Opcode(instruction) == TailLocalCallWithFrame) != 1
            || scanner.Count(instruction => Opcode(instruction) == Swap) != 1
            || returnIndex < 0 || caseEndIndex <= returnIndex)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        object[] Body(string name, int width)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == name && Value(arity) == width
            );

            return info < 0 ? [] : instructions.Skip(info + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
