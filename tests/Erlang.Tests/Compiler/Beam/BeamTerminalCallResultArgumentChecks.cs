using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalCallResultArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-call-results-in-terminal-argument-registers";
    internal const string FrameTestName = "compiler/beam/compacts-dead-caught-input-stack-slot";
    private const string FrameShapeError = "The protected call retained an extra entry argument slot.";
    internal const string TrimTestName = "compiler/beam/trims-owned-return-arm-after-closed-catch";
    private const string TrimShapeError = "The closed catch retained dead stack slots in the returning arm.";
    private const string Module = "beam_terminal_call_result_arguments";
    private const string BranchResultError = "The collecting result must reuse only an input that is dead on both outgoing paths.";
    private const string JoinError = "The terminal argument join retained a temporary stack copy or used it in another argument.";
    private const string ShapeError = "The terminal call kept a stack spill for its local result.";

    private const string Source = """
        -module(beam_terminal_call_result_arguments).
        -export([checked/3]).

        checked(Options = [{_,_}|_], Timeout, Debug) ->
            case catch service:validate(Options) of
                {ok, Checked} ->
                    Address = proplists:get_value(address, Checked),
                    Port = proplists:get_value(port, Checked),
                    Profile = proplists:get_value(profile, Checked, default),
                    Name = local_name(Address, Port, Profile),
                    service:start({local,Name}, service, [undefined,Checked,Timeout,Debug,Address,Port]);
                {error, Reason} ->
                    service:report(Reason),
                    {stop,Reason}
            end.

        local_name(A,B,C) -> service:name(fixture,A,B,C).
        """;

    internal static void Run()
    {
        CheckTerminalJoin();
        CheckBranchCollectingResult();

        string alternate = Source.Replace("{local,Name}, service,", "{local,Name}, Debug,", StringComparison.Ordinal);
        if (alternate == Source || !UsesDirectArguments(Source) || UsesDirectArguments(alternate))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static void CheckTerminalJoin()
    {
        var compiler = new BeamModuleCompiler("terminal_join_fixture");
        int branchTag = (int)Call(compiler, "Intern", "alternate");
        int incomingTag = (int)Call(compiler, "Intern", "incoming");
        byte[] frame =
        [
            .. Start(
                1,
                2,
                1,
                3
            ), Allocate, U(3), U(3), Move, X(0), Y(0), Move, X(1), Y(1), Move, X(2), Y(2)
        ];
        byte[] dispatch = [ExactEqualTest, F(3), X(0), A(branchTag), ExternalCall, U(1), U(0)];
        byte[] jump = [Jump, F(4), Label, U(3)];
        byte[] joined = [Label, U(4)];
        byte[] tail = [Line, U(1), TailExternalCallWithFrame, U(3), U(0), U(3), EndCode];
        byte[] withSpill =
        [
            .. dispatch, .. jump, Move, Y(0), X(0), .. joined,
            Move, X(0), Y(0), Move, Y(1), X(1), Move, Y(0), X(2), Move, Y(2), X(0)
        ];
        byte[] original = [.. frame, .. withSpill, .. tail];
        byte[] expected =
        [
            .. frame, .. dispatch, Move, X(0), X(2), .. jump, Move, Y(0), X(2), .. joined,
            Move, Y(1), X(1), Move, Y(2), X(0), .. tail
        ];
        byte[] extraJoinEdge =
        [
            .. frame, ExactEqualTest, F(4), X(0), A(incomingTag), .. withSpill, .. tail
        ];
        byte[] secondScratchRead = (byte[])original.Clone();
        secondScratchRead[frame.Length + withSpill.Length - 8] = Y(0);
        byte[] collectsAtJoin = [.. frame, .. withSpill, TestHeap, U(2), U(3), .. tail];
        if (Apply(original) is not { } result || !result.AsSpan().SequenceEqual(expected)
            || Apply(extraJoinEdge) is not null || Apply(secondScratchRead) is not null
            || Apply(collectsAtJoin) is not null)
        {
            throw new InvalidOperationException(JoinError);
        }

        byte[]? Apply(byte[] body) => (byte[]?)Call(
            compiler,
            "ForwardTerminalJoinArgument",
            (ReadOnlyMemory<byte>)body,
            5
        );
    }

    private static void CheckBranchCollectingResult()
    {
        var compiler = new BeamModuleCompiler("branch_collecting_result_fixture");
        int branchTag = (int)Call(compiler, "Intern", "alternate");
        byte[] entry = Start(
            1,
            2,
            1,
            3
        );
        byte[] selector = [ExternalCall, U(1), U(0)];
        byte[] branch = [ExactEqualTest, F(3), X(0), A(branchTag), Line, U(1)];
        byte[] producer = [GcBifOne, F(0), U(0), U(0), Y(1), X(0), Move, X(0), Y(0)];
        byte[] hot = [ExternalCall, U(1), U(0), Move, Y(0), X(2), Jump, F(4)];
        byte[] cold = [Label, U(3), Move, Y(2), X(2), Label, U(4), Move, Y(3), X(0), Move, Y(2), X(1)];
        byte[] entrySpill =
        [
            .. entry, Allocate, U(4), U(3), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(2), Y(1), Move, X(1), Y(2), Move, X(0), Y(3),
            .. selector, Move, X(0), Y(0), .. branch
        ];
        byte[] tail = [TailExternalCallWithFrame, U(3), U(0), U(4), EndCode];
        byte[] before = [.. entrySpill, .. producer, .. hot, .. cold, .. tail];
        byte[] expected =
        [
            .. entry, Allocate, U(3), U(3), Move, X(2), Y(0), Move, X(1), Y(1), Move, X(0), Y(2),
            .. selector, .. branch, GcBifOne, F(0), U(0), U(0), Y(0), Y(0), Move, Y(0), X(0),
            ExternalCall, U(1), U(0), Move, Y(0), X(2), Jump, F(4),
            Label, U(3), Move, Y(1), X(2), Label, U(4), Move, Y(2), X(0), Move, Y(1), X(1),
            TailExternalCallWithFrame, U(3), U(0), U(3), EndCode
        ];
        byte[] readsInputOnRetry = (byte[])cold.Clone();
        readsInputOnRetry[3] = Y(1);
        byte[] readsInputAfterResult = [Move, Y(1), X(1), .. hot];
        byte[] branchOnFailure = (byte[])producer.Clone();
        branchOnFailure[1] = F(3);
        if (Apply(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply([.. entrySpill, .. producer, .. hot, .. readsInputOnRetry, .. tail]) is not null
            || Apply([.. entrySpill, .. producer, .. readsInputAfterResult, .. cold, .. tail]) is not null
            || Apply([.. entrySpill, .. branchOnFailure, .. hot, .. cold, .. tail]) is not null)
        {
            throw new InvalidOperationException(BranchResultError);
        }

        byte[]? Apply(byte[] code) => (byte[]?)Call(
            compiler,
            "ReuseBranchCollectingResultSlot",
            (ReadOnlyMemory<byte>)code,
            5
        );
    }

    internal static void RunFrame()
    {
        string liveInput = Source.Replace("service:report(Reason)", "service:report({Reason,Options})", StringComparison.Ordinal);
        if (liveInput == Source || !HasCompactedFrame(Source) || HasCompactedFrame(liveInput))
        {
            throw new InvalidOperationException(FrameShapeError);
        }
    }

    private static bool HasCompactedFrame(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        var allocations = instructions.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        var catches = instructions.Where(instruction => Opcode(instruction) == CatchEnter).ToArray();

        return allocations.Length == 1 && catches.Length == 1
            && Children(allocations[0], "Operands").Select(Value).SequenceEqual([5, 3])
            && Value(Children(catches[0], "Operands")[0]) == 4;
    }

    internal static void RunTrim()
    {
        string twoPayloads = Source.Replace("{stop,Reason}", "{stop,Reason,Timeout}", StringComparison.Ordinal);
        if (twoPayloads == Source || !UsesSingleSlotTrim(Source) || UsesSingleSlotTrim(twoPayloads))
        {
            throw new InvalidOperationException(TrimShapeError);
        }
    }

    private static bool UsesSingleSlotTrim(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        int caught = Array.FindIndex(instructions, instruction => Opcode(instruction) == CatchEnd);
        for (int index = caught + 1; index + 1 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == TrimFrame && Opcode(instructions[index + 1]) == Move
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([4, 1])
                && Children(instructions[index + 1], "Operands").Select(Value).SequenceEqual([0, 0]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesDirectArguments(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        for (int index = 0; index + 3 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == LocalCall && Opcode(instructions[index + 1]) == TestHeap
                && Children(instructions[index + 1], "Operands").Select(Value).SequenceEqual([15, 1])
                && Opcode(instructions[index + 2]) == PutTuple
                && Opcode(instructions[index + 3]) == PutList
                && Children(instructions[index + 3], "Operands").Select(Value).Last() == 1)
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
