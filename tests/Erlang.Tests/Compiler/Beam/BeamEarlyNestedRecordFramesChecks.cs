using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamEarlyNestedRecordFramesChecks
{
    internal const string TestName = "compiler/beam/checks-nested-record-before-allocating-branch";
    private const string WrongSchedule = "The nested record check changed its cold operand, branch frame, or live success argument.";

    internal static Task Run()
    {
        byte[] input = Start(
            1,
            2,
            2,
            2
        );
        byte[] parent = [IsTaggedTuple, F(3), X(1), U(2), A(3)];
        byte[] oldPrefix =
        [
            GetTupleElement, X(1), U(1), X(2), Allocate, U(2), U(3),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, X(1), Y(1), Move, X(2), Y(0), IsTaggedTuple, F(4), Y(0), U(2), A(4),
            GetTupleElement, Y(0), U(1), X(0)
        ];
        byte[] newPrefix =
        [
            GetTupleElement, X(1), U(1), X(0), IsTaggedTuple, F(4), X(0), U(2), A(4),
            Allocate, U(2), U(2), Move, X(0), Y(0), Move, X(1), Y(1), GetTupleElement, X(0), U(1), X(0)
        ];
        byte[] returning = [Deallocate, U(2), Return];
        byte[] oldCold = [Label, U(3), BadRecord, X(1), Label, U(4), Line, U(1), BadRecord, Y(0), EndCode];
        byte[] newCold = [.. oldCold[..^2], X(0), EndCode];
        byte[] before = [.. input, .. parent, .. oldPrefix, .. returning, .. oldCold];
        byte[] expected = [.. input, .. parent, .. newPrefix, .. returning, .. newCold];
        if (Rewrite(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite([.. input, .. parent, .. oldPrefix, Move, X(2), X(0), .. returning, .. oldCold]) is not null
            || Rewrite([.. input, .. parent, .. oldPrefix, Jump, F(4), .. oldCold]) is not null)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] oldSpill =
        [
            GetTupleElement, X(1), U(1), X(2), Allocate, U(3), U(3),
            InitYRegisters, ExtendedListOperand, U(1), Y(2),
            Move, X(1), Y(1), Move, X(2), Y(0), IsTaggedTuple, F(4), Y(0), U(2), A(4),
            GetTupleElement, Y(0), U(1), X(0), Move, X(0), Y(2)
        ];
        byte[] newSpill =
        [
            GetTupleElement, X(1), U(1), X(0), IsTaggedTuple, F(4), X(0), U(2), A(4),
            Allocate, U(3), U(2), Move, X(0), Y(0), Move, X(1), Y(1), GetTupleElement, X(0), U(1), Y(2)
        ];
        byte[] useSpill = [Move, Y(2), X(0), Deallocate, U(3), Return];
        byte[] savedBefore = [.. input, .. parent, .. oldSpill, .. useSpill, .. oldCold];
        byte[] savedExpected = [.. input, .. parent, .. newSpill, .. useSpill, .. newCold];
        if (Rewrite(savedBefore) is not { } savedActual || !savedActual.AsSpan().SequenceEqual(savedExpected)
            || Rewrite([.. input, .. parent, .. oldSpill, TestHeap, U(2), U(1), .. useSpill, .. oldCold]) is not null)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        var compiler = new BeamModuleCompiler("early_nested_record_frames_probe");
        _ = Call(compiler, "Intern", "run");
        _ = Call(compiler, "Intern", "outer");
        _ = Call(compiler, "Intern", "inner");

        return (byte[]?)Call(
            compiler,
            "HoistCheckedNestedRecordAllocations",
            new ReadOnlyMemory<byte>(bytes),
            5
        );
    }
}
