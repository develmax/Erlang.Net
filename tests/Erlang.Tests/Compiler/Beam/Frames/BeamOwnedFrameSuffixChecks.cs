using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOwnedFrameSuffixChecks
{
    internal const string TestName = "compiler/beam/removes-init-only-slots-from-an-owned-nonzero-frame";
    private const string WrongLayout = "The closed frame must release only its live slots and preserve the sibling frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_owned_frame_suffix_probe");
        byte[] before = Fixture();
        byte[] expected = Fixture(compacted: true);
        if (Rewrite(before) is not { } after || !after.AsSpan().SequenceEqual(expected)
            || Rewrite(after) is not null || Rewrite(Fixture(readsLastSlot: true)) is not null
            || Rewrite(Fixture(conditionalExit: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        _ = Call(compiler, "Intern", "copy");
        byte[] hot = CheckedTerminalRecord();
        if (RewriteChecked(hot) is not { } trimmed
            || !trimmed.AsSpan().SequenceEqual(CheckedTerminalRecord(trimmed: true))
            || RewriteChecked(trimmed) is not null
            || RewriteChecked(CheckedTerminalRecord(readsRetiredSlot: true)) is not null
            || RewriteChecked(CheckedTerminalRecord(comparesOtherCarrier: true)) is not null)
        {
            throw new InvalidOperationException("Trim only the dead prefix owned by the checked construction continuation.");
        }

        return Task.CompletedTask;

        byte[]? RewriteChecked(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "TrimOwnedCheckedRecordCallFrames",
            code,
            5
        );

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "TrimUnusedOwnedFrameSuffixes",
            code,
            5
        );
    }

    private static byte[] CheckedTerminalRecord(
        bool trimmed = false,
        bool readsRetiredSlot = false,
        bool comparesOtherCarrier = false
    )
    {
        byte suffix = Y(trimmed ? 0 : readsRetiredSlot ? 0 : 1);
        byte record = Y(trimmed ? 1 : 2);
        byte[] trim = trimmed ? [TrimFrame, U(1), U(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(1), X(1), TestArity, F(1), X(1), U(2),
            Allocate, U(3), U(2), Move, X(0), Y(0), Move, X(1), Y(1), Move, X(1), Y(2),
            ExternalCall, U(1), U(0), GetTupleElement, Y(comparesOtherCarrier ? 2 : 1), U(0), X(1),
            ExactEqualTest, F(3), X(0), X(1), GetTupleElement, Y(1), U(1), X(0),
            .. trim, Line, U(1), ExternalCall, U(1), U(0), TestHeap, U(5), U(1),
            PutList, suffix, X(0), X(0),
            UpdateRecord, A(2), U(2), record, X(0), ExtendedListOperand, U(2), U(2), X(0),
            Deallocate, U(trimmed ? 2 : 3), Return,
            Label, U(3), Move, Y(0), Y(2), TrimFrame, U(2), U(1), Move, Y(0), X(0),
            Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool compacted = false,
        bool readsLastSlot = false,
        bool conditionalExit = false
    )
    {
        int width = compacted ? 1 : 3;
        int liveSlot = readsLastSlot ? 2 : 0;
        byte[] cleared = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(2),
            Y(readsLastSlot ? 0 : 1), Y(readsLastSlot ? 1 : 2)];
        byte[] branch = conditionalExit ? [IsNonemptyList, F(4), X(0)] : [];
        byte[] otherExit = conditionalExit ? [Label, U(4), Deallocate, U(3), Return] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsAtom, F(3), X(0), Allocate, U(width), U(1), Move, X(0), Y(liveSlot),
            .. cleared, .. branch,
            TestHeap, U(2), U(0), PutList, Y(liveSlot), A(0), X(0), Deallocate, U(width), Return,
            .. otherExit,
            Label, U(3), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }
}
