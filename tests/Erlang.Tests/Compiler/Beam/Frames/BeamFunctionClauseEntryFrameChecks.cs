using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFunctionClauseEntryFrameChecks
{
    internal const string TestName = "compiler/beam/delays-the-frame-before-own-function-clause-failure";
    private const string MissingPlacement = "Expected read-only entry tests to fail without a frame and the call path to retain its frame.";
    private const string InvalidPlacement = "Expected a mutating, collecting, shared, or local-target failure path to retain its frame.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte FunctionClause = 0x15;
    private const byte RecursiveEntry = 0x25;
    private const byte Failed = 0x35;
    private const byte LocalExit = 0x45;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] frame = [Allocate, One, Two, Move, X0, Y0];
        byte[] tests = [IsAtom, Failed, X1, ExactEqualTest, Failed, Y0, X1];
        byte[] delayedTests = [IsAtom, Failed, X1, ExactEqualTest, Failed, X0, X1];
        byte[] success = [TestHeap, One, Two, LocalCall, Two, RecursiveEntry, Move, Y0, X0, Deallocate, One, Return];
        byte[] failure = [Label, Three, Line, One, Deallocate, One, Jump, FunctionClause, EndCode];
        byte[] delayedFailure = [Label, Three, Line, One, Jump, FunctionClause, EndCode];
        int testStackRead = entry.Length + frame.Length + 5;
        byte[] original = [.. entry, .. frame, .. tests, .. success, .. failure];
        byte[] expected = [.. entry, .. delayedTests, .. frame, .. success, .. delayedFailure];
        if (Rewrite(original, [(testStackRead, testStackRead + 1, 0)]) is not { } actual
            || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] changedArguments =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, Move, X1, X0, Deallocate, One, Jump, FunctionClause, EndCode
        ];
        byte[] collectingFailure =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, TestHeap, One, Two, Deallocate, One, Jump, FunctionClause, EndCode
        ];
        byte[] sharedFailure =
        [
            .. entry, .. frame, .. tests,
            LocalCall, Two, RecursiveEntry, Move, Y0, X0, Jump, Failed, .. failure
        ];
        byte[] localTarget =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, Deallocate, One, Jump, LocalExit, Label, Four, Return, EndCode
        ];
        if (Rewrite(changedArguments, []) is not null || Rewrite(collectingFailure, []) is not null
            || Rewrite(sharedFailure, []) is not null || Rewrite(localTarget, []) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source, List<(int Start, int End, int Slot)> stackOperands)
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
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            body,
            stackOperands,
            null,
            new Func<object, bool>(static _ => false),
            false
        );
    }
}
