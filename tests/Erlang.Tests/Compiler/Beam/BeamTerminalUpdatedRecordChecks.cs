using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalUpdatedRecordChecks
{
    internal const string TestName = "compiler/beam/retains-updated-record-for-terminal-tuple";
    private const string WrongLayout = "Terminal tuple construction must keep the record result in X0 without changing other frame operands.";

    internal static Task Run()
    {
        byte[] header = Start(
            1,
            2,
            2,
            2
        );
        byte[] reservation =
        [
            AllocateHeap, U(2), U(6), U(2), Move, X(0), Y(0), Move, X(1), Y(1)
        ];
        byte[] update = [UpdateRecord, A(3), U(2), Y(0), X(0), ExtendedListOperand, U(2), U(2), X(1)];
        byte[] save = [Move, X(0), Y(0)];
        byte[] beforeTuple = [PutTuple, X(0), ExtendedListOperand, U(3), A(4), Y(0), A(3)];
        byte[] afterTuple = [PutTuple, X(0), ExtendedListOperand, U(3), A(4), X(0), A(3)];
        byte[] returning = [Deallocate, U(2), Return, EndCode];
        byte[] before = [.. header, .. reservation, .. update, .. save, .. beforeTuple, .. returning];
        byte[] expected = [.. header, .. reservation, .. update, .. afterTuple, .. returning];
        byte[] collects = [TestHeap, U(4), U(0)];
        byte[] protectedEntry = [CatchEnter, Y(1), F(2), CatchEnd, Y(1)];
        byte[] repeated = [PutTuple, X(0), ExtendedListOperand, U(3), A(4), Y(0), Y(0)];
        if (Rewrite(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite([.. header, .. reservation, .. update, .. save, .. collects, .. beforeTuple, .. returning]) is not null
            || Rewrite([.. header, .. reservation, .. update, .. save, .. repeated, .. returning]) is not null
            || Rewrite([.. header, .. reservation, .. protectedEntry, .. update, .. save, .. beforeTuple, .. returning]) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckJoinedReturn(
            header,
            update,
            save,
            beforeTuple,
            afterTuple,
            returning
        );

        return Task.CompletedTask;
    }

    private static void CheckJoinedReturn(
        byte[] header,
        byte[] update,
        byte[] save,
        byte[] beforeTuple,
        byte[] afterTuple,
        byte[] returning
    )
    {
        byte[] frame = [Allocate, U(2), U(2), Move, X(0), Y(0)];
        byte[] initialize = [Move, X(1), Y(1)];
        byte[] alternative = [ExactEqualTest, F(3), X(1), A(4)];
        byte[] join = [Label, U(3)];
        byte[] heap = [TestHeap, U(4), U(0)];
        byte[] liveHeap = [TestHeap, U(4), U(1)];
        byte[] joinSave = [.. save[..^1], Y(1)];
        byte[] joinTuple = [.. beforeTuple[..^2], Y(1), beforeTuple[^1]];
        byte[] prefix = [.. header, .. frame, .. initialize, .. alternative, TestHeap, U(3), U(2), .. update, .. join];
        byte[] before = [.. prefix, .. joinSave, .. heap, .. joinTuple, .. returning];
        byte[] expected = [.. prefix, .. liveHeap, .. afterTuple, .. returning];

        // The fallthrough frame owns Y1 on both branches. Removing the
        // initialization would make Y1 unsafe across the promoted heap test.
        byte[] reservedFrame = [AllocateHeap, U(2), U(3), U(2), Move, X(0), Y(0)];
        byte[] missingInitialization =
        [
            .. header, .. reservedFrame, .. update, .. join, .. joinSave, .. heap, .. joinTuple, .. returning
        ];
        if (Rewrite(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(missingInitialization) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        var compiler = new BeamModuleCompiler("terminal_updated_record_probe");
        _ = Call(compiler, "Intern", "run");
        _ = Call(compiler, "Intern", "reuse");
        _ = Call(compiler, "Intern", "wrapped");

        return (byte[]?)Call(
            compiler,
            "KeepTerminalUpdatedRecordInX",
            new ReadOnlyMemory<byte>(bytes),
            4
        );
    }
}
