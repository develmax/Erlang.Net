using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSingleReturningCallFrameChecks
{
    internal const string TestName = "compiler/beam/uses-zero-slot-frame-for-single-returning-call";
    private const string Fixture = "single_returning_call_frame_probe.erl";
    private const string WrongCallFrame = "Pre-call closure preparation must keep its heap roots and only the return frame.";
    private const string LostSavedValue = "The value read after the call must retain a stack slot.";
    private const string WrongConstructedResult = "The call continuation must keep only its initialized result roots and frame.";

    internal const string Source = """
        -module(single_returning_call_frame_probe).
        -export([list_helper/1, external/1, local/1, captures/2, needs_slot/2, checked/1, discard/1]).

        list_helper(Bytes) ->
            Encode = fun
                (0) -> $x;
                (C) -> C
            end,
            unicode:characters_to_list([Encode(C) || C <- Bytes]).

        external(Bytes) ->
            Encode = fun(C) -> C band 127 end,
            list_to_binary(lists:map(Encode, Bytes)).

        local(Bytes) ->
            Encode = fun(C) -> C band 127 end,
            list_to_binary(map_local(Encode, Bytes)).

        captures(Bytes, Mask) ->
            Encode = fun(C) -> C band Mask end,
            list_to_binary(lists:map(Encode, Bytes)).

        needs_slot(Bytes, Header) ->
            Encode = fun(C) -> C band 127 end,
            Values = lists:map(Encode, Bytes),
            list_to_binary([Header | Values]).

        checked({list, Bytes}) ->
            Encode = fun(C) -> C band 127 end,
            list_to_binary(lists:map(Encode, Bytes)).

        discard(Bytes) ->
            Inspect = fun(C) -> C band 127 end,
            lists:foreach(Inspect, Bytes),
            ok.

        map_local(Fun, Bytes) -> lists:map(Fun, Bytes).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo)
            .ToArray();
        for (int index = 0; index < 4; index++)
        {
            object[] function = instructions[starts[index]..starts[index + 1]];
            object allocation = function.Single(instruction => Opcode(instruction) == AllocateHeap);
            object tail = function.Single(instruction => Opcode(instruction) == TailExternalCallWithFrame);
            object closure = function.Single(instruction => Opcode(instruction) == MakeFun);
            if (Value(Operands(allocation)[0]) != 0 || Value(Operands(tail)[^1]) != 0
                || (string)Property(Operands(closure)[1], "Kind") != "x"
                || function.Count(instruction => Opcode(instruction) is LocalCall or ExternalCall) != 1)
            {
                throw new InvalidOperationException(WrongCallFrame);
            }
        }

        object[] discarded = instructions[starts[6]..starts[7]];
        if (!discarded.Any(instruction => Opcode(instruction) == AllocateHeap && Value(Operands(instruction)[0]) == 0)
            || !discarded.Any(instruction => Opcode(instruction) == Deallocate && Value(Operands(instruction)[0]) == 0)
            || discarded.Count(instruction => Opcode(instruction) == ExternalCall) != 1)
        {
            throw new InvalidOperationException(WrongCallFrame);
        }

        object[] kept = instructions[starts[4]..starts[5]];
        int call = Array.FindIndex(kept, instruction => Opcode(instruction) == ExternalCall);
        if (call < 0 || !kept.Skip(call + 1).Any(instruction => Operands(instruction)
                .Any(operand => (string)Property(operand, "Kind") == "y")))
        {
            throw new InvalidOperationException(LostSavedValue);
        }

        CheckConstructedReturnFrames();

        return Task.CompletedTask;
    }

    private static void CheckConstructedReturnFrames()
    {
        byte[] tuple = [TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0)];
        byte[] tupleAndList =
        [
            TestHeap, U(5), U(1), PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0),
            PutList, X(0), A(0), X(0)
        ];
        byte[] returned = ConstructorFrame(tuple);
        byte[] passed = ConstructorFrame(tupleAndList, tail: true);
        if (RewriteConstructorFrame(returned) is not { } tupleResult
            || !tupleResult.SequenceEqual(ConstructorFrame(tuple, compact: true))
            || RewriteConstructorFrame(passed) is not { } listResult
            || !listResult.SequenceEqual(ConstructorFrame(tupleAndList, compact: true, tail: true)))
        {
            throw new InvalidOperationException(WrongConstructedResult);
        }

        byte[] withSavedValue =
            [TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), Y(0), X(0)];
        if (RewriteConstructorFrame(ConstructorFrame(withSavedValue)) is not null)
        {
            throw new InvalidOperationException(LostSavedValue);
        }

        byte[] withColdEdge =
        [
            .. ConstructorFrame([IsAtom, F(3), X(0), .. tuple]).SkipLast(1),
            Label, U(3), Deallocate, U(1), Return, EndCode
        ];
        if (RewriteConstructorFrame(withColdEdge) is not null)
        {
            throw new InvalidOperationException(WrongConstructedResult);
        }
    }

    private static byte[] ConstructorFrame(byte[] result, bool compact = false, bool tail = false)
    {
        int width = compact ? 0 : 1;
        byte temporary = compact ? X(0) : Y(0);
        byte[] exit = tail
            ? [TailExternalCallWithFrame, U(1), U(1), U(width)]
            : [Deallocate, U(width), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsList, F(1), X(0),
            Allocate, U(width), U(1),
            Move, X(0), temporary,
            Move, temporary, X(0),
            Line, U(1),
            ExternalCall, U(1), U(0),
            .. result,
            .. exit,
            EndCode
        ];
    }

    private static byte[]? RewriteConstructorFrame(ReadOnlyMemory<byte> code)
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            recordStack
        );

        return (byte[]?)Call(
            new BeamModuleCompiler(Fixture),
            "ReallocateLeafFrameRegisters",
            code,
            body,
            stackOperands,
            true,
            null,
            null,
            false,
            null
        );
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");
}
