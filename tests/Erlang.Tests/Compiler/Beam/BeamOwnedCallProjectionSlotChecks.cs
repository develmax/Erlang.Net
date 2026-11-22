using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOwnedCallProjectionSlotChecks
{
    internal const string TestName = "compiler/beam/removes-a-call-projection-slot-before-owned-trims";

    internal static Task Run()
    {
        byte[] original = Fixture();
        byte[] expected = Fixture(rewritten: true);
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(readsOriginalSlot: true)) is not null
            || Rewrite(Fixture(trimRetainsOriginalSlot: true)) is not null)
        {
            throw new InvalidOperationException("Only a dead call argument slot may be removed across the owning trim.");
        }

        if (Rewrite(ComparisonFixture(), deferComparisons: true) is not { } compared
            || !compared.AsSpan().SequenceEqual(ComparisonFixture(rewritten: true))
            || Rewrite(ComparisonFixture(rewritten: true), deferComparisons: true) is not null
            || Rewrite(ComparisonFixture(overwritesCarrier: true), deferComparisons: true) is not null
            || Rewrite(ComparisonFixture(usesSavedFieldOnSuccess: true), deferComparisons: true) is not null)
        {
            throw new InvalidOperationException("Only an owned immutable field may be projected after the call.");
        }

        if (Rewrite(CombinedFixture()) is not { } outgoing
            || !outgoing.AsSpan().SequenceEqual(CombinedFixture(outgoing: true))
            || Rewrite(outgoing, deferComparisons: true) is not { } both
            || !both.AsSpan().SequenceEqual(CombinedFixture(outgoing: true, compared: true))
            || Rewrite(both) is not null || Rewrite(both, deferComparisons: true) is not null)
        {
            throw new InvalidOperationException("The outgoing and tested field slots must both retire.");
        }

        if (Rewrite(LaterCallFixture(), laterCall: true) is not { } later
            || !later.AsSpan().SequenceEqual(LaterCallFixture(rewritten: true))
            || Rewrite(later, laterCall: true) is not null
            || Rewrite(LaterCallFixture(overwritesCarrier: true), laterCall: true) is not null
            || Rewrite(LaterCallFixture(retryReadsField: true), laterCall: true) is not null
            || Rewrite(LaterCallFixture(retainsFieldInTrim: true), laterCall: true) is not null)
        {
            throw new InvalidOperationException("Only a retained tuple may supply a field used solely by the accepted call.");
        }

        if (ForwardFreshCons(ConsBuilder()) is not { } fresh
            || !fresh.AsSpan().SequenceEqual(ConsBuilder(forwarded: true))
            || ForwardFreshCons(ConsBuilder(forwarded: true)) is not null
            || ForwardFreshCons(ConsBuilder(observesTail: true)) is not null
            || ForwardFreshCons(ConsBuilder(observesScratch: true)) is not null)
        {
            throw new InvalidOperationException("Fresh conses use only verified dead constructor carriers.");
        }

        if (DelayCheckedEntry(CarrierFrame()) is not { } delayedFrame
            || !delayedFrame.AsSpan().SequenceEqual(CarrierFrame(delayed: true))
            || DelayCheckedEntry(CarrierFrame(delayed: true)) is not null
            || DelayCheckedEntry(CarrierFrame(readsRejectedScratch: true)) is not null
            || DelayCheckedEntry(CarrierFrame(readsSavedTag: true)) is not null
            || DelayCheckedEntry(CarrierFrame(withSiblingTrim: true)) is not { } compactedFrame
            || !compactedFrame.AsSpan().SequenceEqual(CarrierFrame(delayed: true, withSiblingTrim: true))
            || DelayCheckedEntry(compactedFrame) is not null)
        {
            throw new InvalidOperationException("Rejected checked entry carriers must bypass their owned frame.");
        }

        if (DeferTerminalTail(OwnedTerminalTail()) is not { } reprojectedTail
            || !reprojectedTail.AsSpan().SequenceEqual(OwnedTerminalTail(deferred: true))
            || DeferTerminalTail(reprojectedTail) is not null
            || DeferTerminalTail(OwnedTerminalTail(extraTailRead: true)) is not null
            || DeferTerminalTail(OwnedTerminalTail(clobbersState: true)) is not null)
        {
            throw new InvalidOperationException("Only the checked state may produce a retired terminal tail.");
        }

        if (DelayNilTail(NilTailFrame()) is not { } split
            || !split.AsSpan().SequenceEqual(NilTailFrame(delayed: true))
            || DelayNilTail(split) is not null
            || DelayNilTail(NilTailFrame(sharesFallback: true)) is not null
            || DelayNilTail(NilTailFrame(unknownSpill: true)) is not null)
        {
            throw new InvalidOperationException("Only the independently entered fallback may own a delayed frame.");
        }

        byte[] twoEdges = ComparedField();
        if (ReserveField(twoEdges) is not { } reserved || !reserved.AsSpan().SequenceEqual(ComparedField(reserved: true))
            || ReserveField(reserved) is not null
            || ReserveField(ComparedField(anotherFailure: true)) is not null
            || ReserveField(ComparedField(usesNameOnHotCall: true)) is not null)
        {
            throw new InvalidOperationException("Only the compared field owned by both trim edges may receive a slot.");
        }

        byte[] captured = ComparedSources();
        byte[] alternate = ComparedSources(otherOrigin: true);
        if (ReserveField(captured) is not { } reordered
            || !reordered.AsSpan().SequenceEqual(ComparedSources(reserved: true))
            || ReserveField(alternate) is not { } unchangedOrder
            || !unchangedOrder.AsSpan().SequenceEqual(ComparedSources(reserved: true, otherOrigin: true)))
        {
            throw new InvalidOperationException("Only the checked source origins determine physical comparison-frame order.");
        }

        if (SinkFields(RecordFields()) is not { } sunk || !sunk.AsSpan().SequenceEqual(RecordFields(sunk: true))
            || SinkFields(RecordFields(sunk: true)) is not null
            || SinkFields(RecordFields(readsDuringArithmetic: true)) is not null
            || SinkFields(RecordFields(overwritesCheckedTuple: true)) is not null)
        {
            throw new InvalidOperationException("Only untouched checked fields may sink past arithmetic and its heap check.");
        }

        if (SinkFields(SingleRecordField(), single: true) is not { } single
            || !single.AsSpan().SequenceEqual(SingleRecordField(sunk: true))
            || SinkFields(SingleRecordField(sunk: true), single: true) is not null
            || SinkFields(SingleRecordField(observesHoleOnRetry: true), single: true) is not null)
        {
            throw new InvalidOperationException("The low field register can be reused only when matching retries disregard it.");
        }

        if (ScheduleSiblingFields(SiblingFields()) is not { } grouped
            || !grouped.AsSpan().SequenceEqual(SiblingFields(scheduled: true))
            || ScheduleSiblingFields(SiblingFields(scheduled: true)) is not null
            || ScheduleSiblingFields(SiblingFields(readsBeforeHeap: true)) is not null
            || ScheduleSiblingFields(SiblingFields(clobbersChildEarly: true)) is not null
            || ScheduleSiblingFields(SiblingFields(taggedChild: true)) is not { } repositioned
            || !repositioned.AsSpan().SequenceEqual(SiblingFields(scheduled: true, taggedChild: true)))
        {
            throw new InvalidOperationException("Only a closed ordered child group may defer its projections.");
        }

        if (ScheduleSiblingFields(PayloadFields()) is not { } payload
            || !payload.AsSpan().SequenceEqual(PayloadFields(scheduled: true))
            || ScheduleSiblingFields(PayloadFields(scheduled: true)) is not null
            || ScheduleSiblingFields(PayloadFields(observesCountInTuple: true)) is not null
            || ScheduleSiblingFields(PayloadFields(usesOldX0AfterCount: true)) is not null)
        {
            throw new InvalidOperationException("The original X0 payload and rebuilt count must remain separately owned.");
        }

        return Task.CompletedTask;
    }

    private static byte[] ConsBuilder(
        bool forwarded = false,
        bool observesTail = false,
        bool observesScratch = false
    )
    {
        byte[] save = forwarded ? [] : [Move, X(0), X(2)];
        byte[] replaced = forwarded ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(2)];
        byte[] count = forwarded ? [ExtendedTypedRegisterOperand, X(2), U(1)] : [X(4)];

        return
        [
            .. Start(
                1,
                2,
                1,
                5
            ),
            TestHeap, U(22), U(5), PutList, X(4), X(2), X(forwarded ? 2 : 4),
            PutTuple, X(0), ExtendedListOperand, U(2), X(0), X(observesTail ? 2 : 3),
            PutList, X(0), X(3), X(0), .. save,
            UpdateRecord, A(forwarded ? 2 : 3), U(5), X(1), X(0), ExtendedListOperand, U(2), U(4), .. replaced,
            PutList, X(0), X(observesScratch ? 2 : 3), X(0), .. save,
            UpdateRecord, A(2), U(5), X(1), X(0), ExtendedListOperand, U(4), U(2), .. count, U(4), .. replaced,
            Return, EndCode
        ];
    }

    private static byte[]? ForwardFreshCons(byte[] code)
    {
        var compiler = new BeamModuleCompiler("beam_fresh_cons_probe");
        _ = Call(compiler, "Intern", "copy");
        _ = Call(compiler, "Intern", "reuse");

        return (byte[]?)Call(
            compiler,
            "ForwardFreshConsRecordBuilders",
            new ReadOnlyMemory<byte>(code),
            3
        );
    }

    private static byte[] CarrierFrame(
        bool delayed = false,
        bool readsRejectedScratch = false,
        bool readsSavedTag = false,
        bool withSiblingTrim = false
    )
    {
        bool compacted = delayed && withSiblingTrim;
        byte width = U(compacted ? 2 : 3);
        byte eventSlot = Y(compacted ? 1 : 0);
        byte peerSlot = Y(compacted ? 0 : 1);
        byte fieldSlot = Y(compacted ? 0 : 2);
        byte[] reserve =
        [
            Allocate, width, U(2), InitYRegisters, ExtendedListOperand, U(1), fieldSlot,
            Move, X(0), eventSlot, Move, X(1), peerSlot
        ];
        byte[] selectedJoin = withSiblingTrim ? [Jump, F(8), Label, U(8)] : [];
        byte[] arms = withSiblingTrim ? [U(4), U(2), F(3), U(4), F(7)] : [U(2), U(2), F(3)];
        byte[] sibling = withSiblingTrim
            ? [Label, U(7), Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0),
                Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return]
            : [];
        byte[] checkedPrefix = delayed
            ? [GetTupleElement, X(0), U(0), X(2), ExactEqualTest, F(5), X(2), A(2),
                IsTaggedTuple, F(5), X(1), U(2), A(2), .. reserve, GetTupleElement, eventSlot, U(1), fieldSlot]
            : [.. reserve, GetTupleElement, Y(0), U(0), X(0), ExactEqualTest, F(4), X(0), A(2),
                GetTupleElement, Y(0), U(1), Y(2), IsTaggedTuple, F(4), Y(1), U(2), A(2)];
        byte[] restore = delayed ? []
            : [Label, U(4), Move, Y(0), X(0), Move, Y(1), X(1), Deallocate, U(3), Jump, F(5)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(6), X(0), SelectTupleArity, X(0), F(5), ExtendedListOperand, .. arms,
            Label, U(3), .. checkedPrefix, .. selectedJoin,
            Line, U(1), GcBifTwo, F(0), U(0), U(0), readsSavedTag ? X(0) : fieldSlot, I(1), X(0), Deallocate, width, Return,
            .. restore, Label, U(5), Move, X(readsRejectedScratch ? 2 : 1), X(0), Return,
            Label, U(6), Return, .. sibling, EndCode
        ];
    }

    private static byte[]? DelayCheckedEntry(byte[] code) => (byte[]?)Call(
        new BeamModuleCompiler("beam_entry_carrier_frame_probe"),
        "DelayCheckedEntryCarrierFrames",
        new ReadOnlyMemory<byte>(code),
        9
    );

    private static byte[] OwnedTerminalTail(bool deferred = false, bool extraTailRead = false, bool clobbersState = false)
    {
        byte width = U(deferred ? 2 : 4);
        byte state = Y(deferred ? 1 : 3);
        byte[] init = deferred
            ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)]
            : [InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2)];
        byte[] eager = deferred ? [] : [GetTupleElement, state, U(1), Y(2)];
        byte[] late = deferred ? [GetTupleElement, state, U(1), X(1)] : [];
        byte[] clobber = clobbersState ? [Move, X(0), state] : [];
        byte[] cons = deferred ? [PutList, X(0), X(1), X(0)] : [Move, X(0), Y(1), PutList, Y(1), Y(2), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(5), X(1), U(3), A(2), Allocate, width, U(2), .. init,
            Move, X(1), state, .. eager, Move, X(0), Y(0), .. clobber,
            Jump, F(3), Label, U(3), Line, U(1), ExternalCall, U(1), U(0), TestHeap, U(6), U(1), .. late,
            PutTuple, X(0), ExtendedListOperand, U(2), Y(extraTailRead ? 2 : 0), X(0), .. cons,
            UpdateRecord, A(2), U(3), state, X(0), ExtendedListOperand, U(2), U(2), X(0), Deallocate, width, Return,
            Label, U(5), Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return,
            EndCode
        ];
    }

    private static byte[]? DeferTerminalTail(byte[] code) => (byte[]?)Call(
        new BeamModuleCompiler("beam_owned_terminal_tail_probe"),
        "DeferOwnedTerminalRecordTail",
        new ReadOnlyMemory<byte>(code),
        6
    );

    private static byte[] NilTailFrame(bool delayed = false, bool sharesFallback = false, bool unknownSpill = false)
    {
        byte[] frame = [Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(1), Move, X(1), Y(0)];
        byte[] early = delayed ? [] : frame;
        byte[] late = delayed ? frame : [];
        byte[] tail = delayed ? [TailExternalCall, U(1), U(0)] : [TailExternalCallWithFrame, U(1), U(0), U(2)];
        byte[] alternate = sharesFallback ? [Jump, F(3)] : [Move, Y(0), X(0), Deallocate, U(2), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            .. early, IsNil, F(3), X(0), Move, delayed ? X(1) : Y(unknownSpill ? 1 : 0), X(0),
            Line, U(1), .. tail, Label, U(3), .. late, .. alternate, EndCode
        ];
    }

    private static byte[]? DelayNilTail(byte[] code) => (byte[]?)Call(
        new BeamModuleCompiler("beam_nil_tail_frame_probe"),
        "DelayFramePastNilTailCall",
        new ReadOnlyMemory<byte>(code),
        4
    );

    private static byte[] ComparedField(
        bool reserved = false,
        bool anotherFailure = false,
        bool usesNameOnHotCall = false
    )
    {
        byte local = Y(reserved ? 1 : 0);
        byte carrier = Y(reserved ? 2 : 1);
        byte last = Y(reserved ? 3 : 2);
        byte name = reserved ? Y(0) : X(1);
        byte[] init = reserved ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] firstArg = reserved ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(0)];
        byte[] coldLoad = reserved ? [Move, Y(0), X(1)] : [];
        byte[] branch = anotherFailure ? [IsNil, F(3), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(1), X(1), TestArity, F(1), X(1), U(2),
            Allocate, U(reserved ? 4 : 3), U(2), .. init,
            Move, X(0), local, Move, X(1), carrier, Move, X(1), last, .. branch,
            ExternalCall, U(1), U(0), GetTupleElement, carrier, U(0), name,
            ExactEqualTest, F(3), .. firstArg, name,
            GetTupleElement, carrier, U(1), X(0), Move, name, carrier,
            TrimFrame, U(reserved ? 2 : 1), U(2),
            Line, U(1), LocalCall, U(usesNameOnHotCall ? 2 : 1), F(2),
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), Y(0), X(0),
            Deallocate, U(2), Return,
            Label, U(3), .. coldLoad, Move, local, last, TrimFrame, U(reserved ? 3 : 2), U(1),
            Move, A(0), X(0), Line, U(1), ExternalCall, U(2), U(1),
            Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] ComparedSources(bool reserved = false, bool otherOrigin = false)
    {
        bool ordered = reserved && !otherOrigin;
        byte[] captured = ordered
            ? [Move, X(5), Y(2), Move, X(2), Y(3), Move, X(4), Y(4), Move, X(3), Y(5), Move, X(1), Y(6)]
            : [Move, X(1), Y(reserved ? 6 : 5), Move, X(3), Y(reserved ? 5 : 4),
                Move, X(4), Y(reserved ? 4 : 3), Move, X(5), Y(reserved ? 3 : 2), Move, X(2), Y(reserved ? 2 : 1)];
        byte[] attribute = otherOrigin ? [Move, X(1), X(4)] : [GetTupleElement, X(3), U(1), X(4)];
        byte[] init = reserved ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] firstArg = reserved ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(0)];
        byte[] coldLoad = reserved ? [Move, Y(0), X(1)] : [];
        byte local = Y(reserved ? 1 : 0);
        byte name = reserved ? Y(0) : X(1);
        byte holder = Y(reserved ? 5 : 4);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(5), X(0), SelectTupleArity, X(0), F(5), ExtendedListOperand, U(2), U(2), F(4),
            Label, U(4), IsTaggedTuple, F(5), X(1), U(3), A(1),
            GetTupleElement, X(1), U(1), X(2), IsNonemptyList, F(5), X(2), GetList, X(2), X(3), X(2),
            IsTuple, F(5), X(3), TestArity, F(5), X(3), U(2), .. attribute,
            IsNonemptyList, F(5), X(2), GetList, X(2), X(5), X(2),
            IsTuple, F(5), X(5), TestArity, F(5), X(5), U(2),
            Allocate, U(reserved ? 7 : 6), U(6), .. init, .. captured,
            GetTupleElement, X(0), U(1), local, Move, local, X(0),
            ExternalCall, U(1), U(0), GetTupleElement, holder, U(0), name,
            ExactEqualTest, F(3), .. firstArg, name, GetTupleElement, holder, U(1), X(0), Move, name, holder,
            TrimFrame, U(reserved ? 2 : 1), U(5),
            Line, U(1), LocalCall, U(1), F(2), TestHeap, U(7), U(1),
            PutTuple, X(0), ExtendedListOperand, U(6), Y(ordered ? 1 : 0), Y(ordered ? 0 : 1),
            Y(2), Y(3), Y(4), X(0), Deallocate, U(5), Return,
            Label, U(3), .. coldLoad, Move, local, Y(reserved ? 6 : 5), TrimFrame, U(reserved ? 6 : 5), U(1),
            Move, A(0), X(0), Line, U(1), ExternalCall, U(2), U(1), Deallocate, U(1), Return,
            Label, U(5), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[] RecordFields(
        bool sunk = false,
        bool readsDuringArithmetic = false,
        bool overwritesCheckedTuple = false
    )
    {
        byte[] outer = [GetTupleElement, X(1), U(1), X(sunk ? 6 : 4)];
        byte[] inner = [GetTupleElement, X(2), U(2), X(5)];
        byte[] before = sunk ? [] : [.. outer, .. inner];
        byte[] after = sunk ? [.. inner, .. outer] : [];
        byte[] overwrite = overwritesCheckedTuple ? [Move, X(0), X(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            IsTaggedTuple, F(3), X(1), U(4), A(2), IsTaggedTuple, F(3), X(2), U(4), A(3),
            .. overwrite, .. before, Line, U(1),
            GcBifTwo, F(0), U(sunk ? 4 : 6), U(0), X(readsDuringArithmetic ? 4 : 3), I(1), X(sunk ? 4 : 6),
            TestHeap, U(14), U(sunk ? 5 : 7), .. after,
            PutList, X(sunk ? 4 : 6), A(0), X(sunk ? 4 : 6),
            PutTuple, X(0), ExtendedListOperand, U(3), X(sunk ? 6 : 4), X(5), X(0),
            PutList, X(0), A(0), X(0),
            UpdateRecord, A(2), U(4), X(1), X(0), ExtendedListOperand, U(4), U(2), X(sunk ? 4 : 6), U(4), X(0),
            Return, Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[] SingleRecordField(bool sunk = false, bool observesHoleOnRetry = false)
    {
        byte counters = X(sunk ? 2 : 4);
        byte children = X(sunk ? 4 : 6);
        byte child = X(sunk ? 6 : 7);
        byte field = X(sunk ? 8 : 2);
        byte result = X(sunk ? 7 : 8);
        byte[] projection = [GetTupleElement, child, U(1), field];
        byte[] before = sunk ? [] : projection;
        byte[] after = sunk ? projection : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            IsTaggedTuple, F(3), X(1), U(4), A(2),
            GetTupleElement, X(1), U(1), counters, IsNonemptyList, F(3), counters,
            GetTupleElement, X(1), U(2), children, GetList, counters, X(5), counters,
            IsNonemptyList, F(3), children, GetList, children, child, children,
            IsTaggedTuple, F(3), child, U(4), A(3),
            .. before, Line, U(1), GcBifTwo, F(0), U(sunk ? 7 : 8), U(0), X(5), I(1), result,
            TestHeap, U(14), U(sunk ? 8 : 9), .. after,
            PutList, result, counters, result, PutList, X(0), field, X(0), PutList, X(0), children, X(0),
            UpdateRecord, A(2), U(4), X(1), X(0), ExtendedListOperand, U(4), U(2), result, U(3), X(0),
            Return, Label, U(3), Move, X(observesHoleOnRetry ? 2 : 1), X(0), Return, EndCode
        ];
    }

    private static byte[] SiblingFields(
        bool scheduled = false,
        bool taggedChild = false,
        bool readsBeforeHeap = false,
        bool clobbersChildEarly = false
    )
    {
        byte list = X(scheduled ? 3 : 4);
        byte position = X(scheduled ? 4 : 3);
        byte result = X(scheduled || taggedChild ? 6 : 9);
        byte[] check = taggedChild
            ? [IsTaggedTuple, F(3), X(5), U(3), A(3)]
            : [IsTuple, F(3), X(5), TestArity, F(3), X(5), U(3)];
        byte[] outer = [GetTupleElement, X(1), U(3), X(taggedChild ? 8 : scheduled ? 9 : 6)];
        byte[] child = taggedChild
            ? [GetTupleElement, X(5), U(0), X(7)]
            : [GetTupleElement, X(5), U(0), X(clobbersChildEarly ? 5 : 7),
                GetTupleElement, X(5), U(1), X(8), GetTupleElement, X(5), U(2), X(5)];
        byte[] early = scheduled || taggedChild ? [] : [.. outer, .. child];
        byte[] late = scheduled || taggedChild ? [.. child, .. outer] : [];
        byte live = U(scheduled || taggedChild ? 6 : 9);
        byte heapLive = U(scheduled || taggedChild ? 7 : 10);
        byte tags = X(taggedChild ? 8 : scheduled ? 9 : 6);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(3), X(1), U(5), A(2),
            GetTupleElement, X(1), U(1), X(2), IsNonemptyList, F(3), X(2),
            GetTupleElement, X(1), U(2), list, GetList, X(2), position, X(2),
            IsNonemptyList, F(3), list, GetList, list, X(5), list, .. check,
            .. early, Line, U(1), GcBifTwo, F(0), live, U(0), readsBeforeHeap ? tags : position, I(1), result,
            TestHeap, U(20), heapLive, .. late,
            PutList, result, X(2), result, PutTuple, X(0), ExtendedListOperand, U(5), X(0), tags, position, X(5), X(7),
            PutList, X(0), list, X(0), UpdateRecord, A(2), U(5), X(1), X(0), ExtendedListOperand,
            U(4), U(2), result, U(3), X(0),
            Return, Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[] PayloadFields(
        bool scheduled = false,
        bool observesCountInTuple = false,
        bool usesOldX0AfterCount = false
    )
    {
        byte list = X(scheduled ? 3 : 4);
        byte position = X(scheduled ? 4 : 3);
        byte[] fields =
        [
            GetTupleElement, X(5), U(0), X(6), GetTupleElement, X(5), U(1), X(7), GetTupleElement, X(5), U(2), X(5)
        ];
        byte[] early = scheduled ? [] : [.. fields, Move, X(0), X(8)];
        byte[] late = scheduled ? fields : [];
        byte[] count = scheduled ? [PutList, position, X(2), X(2)] : [PutList, position, X(2), X(0), Move, X(0), position];
        byte[] nested = scheduled ? [PutList, X(0), list, X(0)]
            : [Move, X(0), X(2), PutList, X(2), list, X(0), Move, X(0), X(2)];
        byte[] firstValue = scheduled ? [ExtendedTypedRegisterOperand, X(2), U(1)] : [position];
        byte[] secondValue = scheduled ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(3), X(1), U(5), A(2),
            GetTupleElement, X(1), U(1), X(2), IsNonemptyList, F(3), X(2),
            GetTupleElement, X(1), U(2), list, GetList, X(2), position, X(2),
            IsNonemptyList, F(3), list, GetList, list, X(5), list,
            IsTuple, F(3), X(5), TestArity, F(3), X(5), U(3), .. early, Line, U(1),
            GcBifTwo, F(0), U(scheduled ? 6 : 9), U(0), position, I(1), position,
            TestHeap, U(16), U(scheduled ? 6 : 9), .. late, .. count,
            PutList, X(scheduled ? 0 : 8), X(usesOldX0AfterCount ? 0 : 5), X(0),
            PutTuple, X(0), ExtendedListOperand, U(3), X(6), observesCountInTuple ? position : X(7), X(0), .. nested,
            UpdateRecord, A(4), U(5), X(1), X(0), ExtendedListOperand,
            U(4), U(2), .. firstValue, U(3), .. secondValue,
            Return, Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[]? ScheduleSiblingFields(byte[] code) => SinkFields(
        code,
        pass: "ScheduleSiblingListProjectionGroup"
    );

    private static byte[]? SinkFields(byte[] code, bool single = false, string? pass = null)
    {
        var compiler = new BeamModuleCompiler("beam_sunk_record_field_probe");
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "+",
            2
        );
        _ = Call(compiler, "Intern", "copy");

        return (byte[]?)Call(
            compiler,
            pass ?? (single ? "SinkSingleOwnedRecordConstructionField" : "SinkCheckedRecordConstructionFields"),
            new ReadOnlyMemory<byte>(code),
            4
        );
    }

    private static byte[]? ReserveField(byte[] code)
    {
        var compiler = new BeamModuleCompiler("beam_compared_field_slot_probe");
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "list_to_atom",
            1
        );
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "++",
            2
        );

        return (byte[]?)Call(
            compiler,
            "ReserveComparedFieldSlot",
            new ReadOnlyMemory<byte>(code),
            6
        );
    }

    private static byte[] Fixture(
        bool rewritten = false,
        bool readsOriginalSlot = false,
        bool trimRetainsOriginalSlot = false
    )
    {
        byte frame = U(rewritten ? 2 : 3);
        byte preserved = Y(rewritten ? 1 : 2);
        byte slot = Y(trimRetainsOriginalSlot ? 1 : 0);
        byte[] copy = rewritten ? [] : [Move, slot, X(0)];
        byte[] trim = trimRetainsOriginalSlot
            ? [TrimFrame, U(1), U(2)]
            : [TrimFrame, U(rewritten ? 1 : 2), U(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, frame, U(2), Move, X(1), preserved,
            InitYRegisters, ExtendedListOperand, U(1), Y(trimRetainsOriginalSlot || rewritten ? 0 : 1),
            GetTupleElement, X(0), U(1), rewritten ? X(0) : slot,
            .. copy, LocalCall, U(1), F(2),
            ExactEqualTest, F(3), X(0), A(2),
            Move, readsOriginalSlot ? slot : preserved, X(0),
            Deallocate, frame, Return,
            Label, U(3), .. trim, Move, Y(0), X(0),
            Deallocate, U(trimRetainsOriginalSlot ? 2 : 1), Return, EndCode
        ];
    }

    private static byte[] ComparisonFixture(
        bool rewritten = false,
        bool overwritesCarrier = false,
        bool usesSavedFieldOnSuccess = false
    )
    {
        byte frame = U(rewritten ? 2 : 3);
        byte carrier = Y(rewritten ? 1 : 2);
        byte[] before = rewritten ? [] : [GetTupleElement, X(1), U(1), Y(0)];
        byte[] change = overwritesCarrier ? [Move, X(0), carrier] : [];
        byte[] after = rewritten ? [GetTupleElement, carrier, U(1), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, frame, U(2),
            InitYRegisters, ExtendedListOperand, U(1), Y(rewritten ? 0 : 1),
            Move, X(1), carrier,
            .. before, .. change, LocalCall, U(1), F(2), .. after,
            ExactEqualTest, F(3), X(0), rewritten ? X(1) : Y(0),
            Move, usesSavedFieldOnSuccess ? Y(0) : carrier, X(0),
            Deallocate, frame, Return,
            Label, U(3), TrimFrame, U(rewritten ? 1 : 2), U(1), Move, Y(0), X(0),
            Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] CombinedFixture(bool outgoing = false, bool compared = false)
    {
        int width = compared ? 2 : outgoing ? 3 : 4;
        byte carrier = Y(width - 1);
        byte[] comparisonSave = compared ? [] : [GetTupleElement, X(1), U(1), Y(0)];
        byte[] incoming = outgoing
            ? [GetTupleElement, X(0), U(2), X(0)]
            : [GetTupleElement, X(0), U(2), Y(1), Move, Y(1), X(0)];
        byte[] projection = compared ? [GetTupleElement, carrier, U(1), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(width), U(2),
            InitYRegisters, ExtendedListOperand, U(1), Y(width - 2),
            Move, X(1), carrier, .. comparisonSave, .. incoming,
            LocalCall, U(1), F(2), .. projection,
            ExactEqualTest, F(3), X(0), compared ? X(1) : Y(0),
            Move, carrier, X(0), Deallocate, U(width), Return,
            Label, U(3), TrimFrame, U(width - 1), U(1), Move, Y(0), X(0),
            Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] LaterCallFixture(
        bool rewritten = false,
        bool overwritesCarrier = false,
        bool retryReadsField = false,
        bool retainsFieldInTrim = false
    )
    {
        int width = rewritten ? 2 : 3;
        byte carrier = Y(width - 1);
        byte slot = Y(1);
        byte[] before = rewritten ? [] : [GetTupleElement, X(1), U(2), slot];
        byte[] overwrite = overwritesCarrier ? [Move, X(0), carrier] : [];
        byte[] after = rewritten ? [GetTupleElement, carrier, U(2), X(0)] : [Move, slot, X(0)];
        byte[] retry = retryReadsField ? [Move, slot, X(0)] : [];
        int trimmed = rewritten || retainsFieldInTrim ? 1 : 2;

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(1), X(1), U(3), A(2),
            Allocate, U(width), U(2),
            InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(1), carrier, .. before, .. overwrite,
            LocalCall, U(1), F(2),
            GetTupleElement, carrier, U(1), X(1),
            ExactEqualTest, F(3), X(0), X(1),
            .. after, ExternalCall, U(1), U(0),
            Deallocate, U(width), Return,
            Label, U(3), .. retry, TrimFrame, U(trimmed), U(width - trimmed),
            Move, Y(0), X(0), Deallocate, U(width - trimmed), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes, bool deferComparisons = false, bool laterCall = false)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            recordStack
        );

        if (laterCall)
        {
            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "RewriteOwnedLaterCallProjectionSlot",
                code,
                body,
                stack
            );
        }

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteOwnedCallProjectionSlot",
            code,
            body,
            stack,
            deferComparisons
        );
    }
}
