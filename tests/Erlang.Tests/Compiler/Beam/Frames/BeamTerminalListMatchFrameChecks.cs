using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalListMatchFrameChecks
{
    internal const string TestName = "compiler/beam/delays-the-frame-past-a-terminal-list-match-failure";
    private const string MissingPlacement = "Expected the list check and its terminal badmatch to run without a frame.";
    private const string TuplePlacement = "The public tuple checks must preserve the original badmatch payload and line without a frame.";
    private const string InvalidPlacement = "A changed input, intervening heap check, or shared failure entry must retain the frame.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte RecursiveEntry = 0x25;
    private const byte Failed = 0x35;
    private const byte OtherFailure = 0x45;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] frame = [Allocate, Zero, Two];
        byte[] test = [IsNonemptyList, Failed, X1];
        byte[] success = [LocalCall, Two, RecursiveEntry, Deallocate, Zero, Return];
        byte[] error = [Label, Three, Move, X1, X0, Line, One, BadMatch, X0, EndCode];
        byte[] original = [.. entry, .. frame, .. test, .. success, .. error];
        byte[] expected = [.. entry, .. test, .. frame, .. success, .. error];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] savedValueFrame = [Allocate, One, Two, Move, X0, Y0];
        byte[] savedValueSuccess = [LocalCall, Two, RecursiveEntry, Move, Y0, X0, Deallocate, One, Return];
        byte[] savedValue = [.. entry, .. savedValueFrame, .. test, .. savedValueSuccess, .. error];
        byte[] savedValueExpected = [.. entry, .. test, .. savedValueFrame, .. savedValueSuccess, .. error];
        if (Rewrite(savedValue) is not { } savedValueActual || !savedValueActual.AsSpan().SequenceEqual(savedValueExpected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] otherInput =
        [
            ..entry, ..frame, ..test, ..success,
            Label, Three, Move, X0, X0, BadMatch, X0, EndCode
        ];
        byte[] interveningHeapCheck =
        [
            .. entry, .. frame, .. test, .. success,
            Label, Three, Move, X1, X0, TestHeap, One, One, BadMatch, X0, EndCode
        ];
        byte[] sharedFailure =
        [
            .. entry, .. frame, .. test,
            LocalCall, Two, RecursiveEntry, Jump, Failed, .. error
        ];
        if (Rewrite(otherInput) is not null || Rewrite(interveningHeapCheck) is not null || Rewrite(sharedFailure) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        byte[] tupleFrame = [Allocate, One, Two, Move, X1, Y0];
        byte[] tupleFailure = [Label, Three, Move, Y0, X0, Line, One, BadMatch, X0, EndCode];
        byte[][] tupleChecks =
        [
            [IsTuple, Failed, Y0, TestArity, Failed, Y0, Two],
            [IsTaggedTuple, Failed, Y0, Two, Module]
        ];
        byte[][] unframedChecks =
        [
            [IsTuple, Failed, X1, TestArity, Failed, X1, Two],
            [IsTaggedTuple, Failed, X1, Two, Module]
        ];
        for (int index = 0; index < tupleChecks.Length; index++)
        {
            byte[] tuple = [.. entry, .. tupleFrame, .. tupleChecks[index], .. savedValueSuccess, .. tupleFailure];
            byte[] tupleExpected = [.. entry, .. unframedChecks[index], .. tupleFrame, .. savedValueSuccess, .. error];
            if (Rewrite(tuple, publicEntry: true) is not { } tupleActual
                || !tupleActual.AsSpan().SequenceEqual(tupleExpected)
                || Rewrite(tuple) is not null)
            {
                throw new InvalidOperationException(TuplePlacement);
            }
        }

        byte[] differentTargets =
        [
            .. entry, .. tupleFrame,
            IsTuple, Failed, Y0, TestArity, OtherFailure, Y0, Two, .. savedValueSuccess,
            Label, Three, Move, Y0, X0, Line, One, BadMatch, X0,
            Label, Four, Move, Y0, X0, Line, Two, BadMatch, X0, EndCode
        ];
        if (Rewrite(differentTargets, publicEntry: true) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source, bool publicEntry = false)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            instructions
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> stack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            stack
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            body,
            stackOperands,
            null,
            new Func<object, bool>(_ => publicEntry),
            false
        );
    }
}
