using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalCallProjectionChecks
{
    internal const string TestName = "compiler/beam/forwards-projections-to-terminal-call-arguments";
    internal const string CompactFrameTestName = "compiler/beam/compacts-owned-frames-before-complete-trims";
    internal const string SharedReturnsTestName = "compiler/beam/shares-terminal-returns-in-mixed-tuple-dispatch";
    private const string FrameShapeError = "The complete trim kept an unused slot or modified the protected frame.";
    private const string SharedReturnsError = "The mixed decision did not share only identical terminal arms.";
    internal const string MixedTupleTableTestName = "compiler/beam/dispatch-reorders-relocated-atom-and-import-uses";
    private const string TableOrderError = "The relocated atom kept its pre-dispatch atom index.";
    internal const string ColdOwnershipTestName = "compiler/beam/keeps-live-entry-alias-and-delays-cold-case-failure";
    private const string ColdOwnershipError = "The mixed decision left a dead entry alias or an early case failure.";
    private const string Module = "beam_terminal_call_projection";
    private const string ShapeError = "The terminal call retained stack projections after constructing its arguments.";

    private const string Source = """
        -module(beam_terminal_call_projection).
        -export([reply/1, protected/1]).
        -record(entry, {kind, rows, other}).

        reply(Entry) ->
            case Entry#entry.kind of
                proceed ->
                    case proplists:get_value(state, Entry#entry.rows) of
                        {_, _, _} -> {ok, Entry#entry.rows};
                        _ ->
                            case proplists:get_value(payload, Entry#entry.rows) of
                                undefined -> fallback(Entry);
                                {sent, _, _} -> {ok, Entry#entry.rows};
                                {packet, Data, _} ->
                                    {ok, lists:keyreplace(payload, 1, Entry#entry.rows, {payload, Data, done})};
                                {_, _} -> {ok, Entry#entry.rows}
                            end
                    end;
                _ -> {ok, Entry#entry.rows}
            end.

        protected(Entry) ->
            case proplists:get_value(payload, Entry#entry.rows) of
                {packet, Data, _} ->
                    try lists:keyreplace(payload, 1, Entry#entry.rows, {payload, Data, done}) of
                        Updated -> {ok, Updated}
                    catch error:_ -> {kept, Entry#entry.rows}
                    end;
                _ -> {ok, Entry#entry.rows}
            end.

        fallback(Entry) -> {other, Entry#entry.other}.
        """;

    internal static void Run()
    {
        var body = ReadBodies();
        if (!HasForwardedProjection(body("reply")) || HasForwardedProjection(body("protected")))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    internal static void CheckCompactFrame()
    {
        var body = ReadBodies();
        object[] reply = body("reply");
        object[] protectedReply = body("protected");
        if (!HasOperands(
            reply,
            Allocate,
            1,
            1
        ) || !HasOperands(
            reply,
            TrimFrame,
            1,
            0
        )
            || HasOperands(
                reply,
                Allocate,
                2,
                1
            ) || HasOperands(
                reply,
                TrimFrame,
                2,
                0
            )
            || !protectedReply.Any(instruction => Opcode(instruction) == TryEnter)
            || protectedReply.Any(instruction => Opcode(instruction) == TrimFrame))
        {
            throw new InvalidOperationException(FrameShapeError);
        }
    }

    internal static void CheckColdOwnership()
    {
        object[] reply = ReadBodies()("reply");
        int firstCall = Array.FindIndex(reply, instruction => Opcode(instruction) == ExternalCall);
        int caseEnd = Array.FindIndex(reply, instruction => Opcode(instruction) == CaseEnd);
        int lastReturn = Array.FindLastIndex(reply, instruction => Opcode(instruction) == Return);
        if (firstCall < 0 || lastReturn < 0 || caseEnd <= lastReturn
            || !reply.Take(firstCall).Zip(reply.Skip(1)).Any(pair =>
                Opcode(pair.First) == Move && Opcode(pair.Second) == GetTupleElement
                && Children(pair.First, "Operands") is [var input, _]
                && Children(pair.Second, "Operands")[0].Equals(input)))
        {
            throw new InvalidOperationException(ColdOwnershipError);
        }
    }

    internal static void CheckMixedTupleTableOrder()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int undefined = Array.IndexOf(atoms, "undefined");
        int sent = Array.IndexOf(atoms, "sent");
        int keyreplace = Array.IndexOf(atoms, "keyreplace");
        if (sent < 0 || keyreplace < 0 || undefined <= sent || undefined <= keyreplace)
        {
            throw new InvalidOperationException(TableOrderError);
        }
    }

    internal static void CheckMixedTupleReturns()
    {
        const string sharedClause = "{sent, _, _} -> {ok, Entry#entry.rows}";
        string different = Source.Replace(sharedClause, "{sent, _, _} -> {unchanged, Entry#entry.rows}", StringComparison.Ordinal);
        if (different == Source || !HasSharedTupleReturn(ReadBodies()("reply"))
            || HasSharedTupleReturn(ReadBodies(different)("reply")))
        {
            throw new InvalidOperationException(SharedReturnsError);
        }
    }

    private static bool HasSharedTupleReturn(object[] instructions)
    {
        object? arity = instructions.FirstOrDefault(instruction => Opcode(instruction) == SelectTupleArity);
        object? tags = instructions.FirstOrDefault(instruction => Opcode(instruction) == SelectValue);
        if (arity is null || tags is null
            || Children(arity, "Operands") is not [var input, _, _, _, var shared, _, _]
            || Children(tags, "Operands") is not [_, _, _, _, var first, _, var second])
        {
            return false;
        }

        return input.GetType().GetProperty("TypeIndex")?.GetValue(input) is not null
            && (Value(shared) == Value(first) || Value(shared) == Value(second));
    }

    private static Func<string, object[]> ReadBodies(string source = Source)
    {
        byte[] beam = CodeGeneration.CompileBeamModule(source, Module + ".erl");
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return Body;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasOperands(object[] instructions, byte opcode, params int[] operands) =>
        instructions.Any(instruction => Opcode(instruction) == opcode
            && Children(instruction, "Operands").Select(Value).SequenceEqual(operands));

    private static bool HasForwardedProjection(object[] instructions)
    {
        for (int index = 0; index + 6 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == TestHeap
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([4, 1])
                && Opcode(instructions[index + 1]) == GetTupleElement
                && Children(instructions[index + 1], "Operands").Select(Value).Last() == 0
                && Opcode(instructions[index + 2]) == PutTuple
                && Value(Children(instructions[index + 2], "Operands")[0]) == 3
                && Opcode(instructions[index + 3]) == GetTupleElement
                && Children(instructions[index + 3], "Operands").Select(Value).Last() == 2
                && Opcode(instructions[index + 4]) == Move && Opcode(instructions[index + 5]) == TrimFrame
                && Children(instructions[index + 5], "Operands").Select(Value).Last() == 0
                && Opcode(instructions[index + 6]) == Move)
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
