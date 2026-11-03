using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class MutableSequenceToken550Checks
{
    public static async Task ActualMutableTokenAndEnclosingRoots()
    {
        foreach (bool owned in new[] { false, true })
            foreach (bool prepared in new[] { false, true })
            {
                await using var runtime = NativeOwned516Profile.Create(owned);
                await CpuStatisticsAccess.Actor<Term>(runtime, context =>
                {
                    Exercise(runtime, context, prepared);

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                await ExercisePacket(runtime);
            }
        await SequenceSelection170Checks.Run();
    }

    private static async Task ExercisePacket(ProcessRuntime runtime)
    {
        var initial = new SequenceTraceToken(
            SequenceTraceFlags.None,
            Term.I(0),
            0,
            Term.A(MutableSequenceToken550CaseNames.SenderAtom),
            0
        );
        await using MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedToken814Async(
            runtime,
            null,
            Nil.Value,
            initial
        );
        ProcessTermHeap506 heap = fragment.HeapOwner;
        SequenceTraceToken token = fragment.Token!;
        TupleTerm physical = token.PhysicalTuple!;
        lock (heap.Gate)
            Require(MailboxBackingProbe.Storage(physical)!.IsMutableSequenceToken814
                && !MailboxBackingProbe.Storage(physical)!.HasImmutablePlainClosure550
                && !MailboxBackingProbe.Storage(physical)!.HasImmutableLiteralOnlyClosure132
                && !MailboxBackingProbe.Storage(physical)!.HasImmutableReadonlyCaptureClosure138
                && MailboxBackingProbe.Storage(physical)!.AllocationWords526 == SequenceTraceRootNames.Arity + 1
                && fragment.StorageSnapshot.LivePayloadWords == SequenceTraceRootNames.Arity
                && heap.PhysicalWordSnapshot526.LivePhysicalWords == SequenceTraceRootNames.Arity + 1);
        ProcessTermHeapSnapshot506 before = heap.Snapshot;
        Pid sender = runtime.InitializationPid;
        SequenceTraceToken next = heap.RewriteSequenceToken(physical, token, token with
        {
            Sender = sender,
            Serial = 1
        });
        Require(ReferenceEquals(next.PhysicalTuple, physical) && ReferenceEquals(next.Sender, sender)
            && heap.Snapshot == before);
    }

    private static void Exercise(ProcessRuntime runtime, ProcessContext context, bool prepared)
    {
        object runtimeGate = typeof(ProcessRuntime).GetField(MutableSequenceToken550CaseNames.RuntimeGateField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        ProcessTermHeap506 heap = context.TermHeap506;
        ProcessTermFrame506 roots = heap.Enter();
        SequenceTraceStorage? pending = null;
        try
        {
            using (ProcessTermFrame506 immediates = heap.Enter())
            {
                int immediateSlot = immediates.Reserve();
                Term input = Term.I(MutableSequenceToken550CaseNames.ImmediateValue);
                ProcessBinaryTraversalSnapshot514 beforeImmediate = runtime.BinaryRegistry512.TraversalSnapshot514;
                immediates.Set(immediateSlot, input);
                ProcessBinaryTraversalSnapshot514 afterImmediate = runtime.BinaryRegistry512.TraversalSnapshot514;
                Require(afterImmediate.VisitedTerms == beforeImmediate.VisitedTerms + 1
                    && afterImmediate.RetainOperations == beforeImmediate.RetainOperations + 1
                    && afterImmediate.LiveBackingReferences == beforeImmediate.LiveBackingReferences);
                Require(immediates.Read(immediateSlot).Equals(input) && !ReferenceEquals(immediates.Read(immediateSlot), input));
                Term detachedImmediate = heap.Export(immediates, input);
                Require(detachedImmediate.Equals(input) && !ReferenceEquals(detachedImmediate, input)
                    && ReferenceEquals(heap.Export(immediates, Nil.Value), Nil.Value));
                immediates.SetPrepared(immediateSlot, input);
                Require(ReferenceEquals(immediates.Read(immediateSlot), input));
                immediates.SetLocalGraph507(immediateSlot, input);
                Require(ReferenceEquals(immediates.Read(immediateSlot), input));
                immediates.Set(immediateSlot, Nil.Value);
                Require(ReferenceEquals(immediates.Read(immediateSlot), Nil.Value));
                Term boxed = new Integer(System.Numerics.BigInteger.One << MutableSequenceToken550CaseNames.BoxedShift);
                immediates.Set(immediateSlot, boxed);
                Require(MailboxBackingProbe.Storage(immediates.Read(immediateSlot)) is not null
                    && immediates.Read(immediateSlot).Equals(boxed));
                Term detachedBoxed = heap.Export(immediates, immediates.Read(immediateSlot));
                Require(detachedBoxed.Equals(boxed) && MailboxBackingProbe.Storage(detachedBoxed) is null);
                immediates.Clear(immediateSlot);
            }
            lock (heap.Gate) heap.CollectGeneration508(true, 0);
            var initial = new SequenceTraceToken(
                SequenceTraceFlags.None,
                Term.I(0),
                0,
                Term.A(MutableSequenceToken550CaseNames.SenderAtom),
                0
            );
            pending = prepared ? SequenceTraceStorage.CreatePrepared(context, initial)
                : SequenceTraceStorage.CreateLocal(context, initial);
            lock (runtimeGate)
            {
                context.SequentialTrace.InstallPrepared(pending);
                pending = null;
            }
            TupleTerm token = context.SequentialTrace.Token!.PhysicalTuple!;
            int pureSlot = roots.Reserve(), enclosingSlot = roots.Reserve(), consSlot = roots.Reserve(), boxedSlot = roots.Reserve();
            var pure = (TupleTerm)roots.CreateTupleInto507(pureSlot, ((TupleTerm)initial.ToTerm()).Items.ToArray());
            var enclosing = (TupleTerm)roots.CreateTupleInto507(enclosingSlot, new Term[] { token });
            Term cons = roots.CreateConsInto507(consSlot, token, Nil.Value);
            var boxedShape = (TupleTerm)roots.CreateTupleInto507(boxedSlot,
                ((TupleTerm)(initial with { Label = token }).ToTerm()).Items.ToArray());
            long words;
            lock (heap.Gate)
            {
                ProcessTermSequence506 tokenBlock = MailboxBackingProbe.Storage(token)!;
                ProcessTermSequence506 pureBlock = MailboxBackingProbe.Storage(pure)!;
                ProcessTermSequence506 enclosingBlock = MailboxBackingProbe.Storage(enclosing)!;
                ProcessTermSequence506 consBlock = MailboxBackingProbe.Storage(cons)!;
                ProcessTermSequence506 boxedBlock = MailboxBackingProbe.Storage(boxedShape)!;
                Require(tokenBlock.AllocationWords526 == SequenceTraceRootNames.Arity + 1
                    && tokenBlock.IsMutableSequenceToken814 && !tokenBlock.HasImmutablePlainClosure550
                    && !boxedBlock.IsMutableSequenceToken814 && !boxedBlock.HasImmutablePlainClosure550
                    && !pureBlock.IsMutableSequenceToken814 && pureBlock.HasImmutablePlainClosure550
                    && !enclosingBlock.HasImmutablePlainClosure550 && !consBlock.HasImmutablePlainClosure550
                    && pureBlock.HasImmutableLiteralOnlyClosure132
                    && !tokenBlock.HasImmutableLiteralOnlyClosure132 && !boxedBlock.HasImmutableLiteralOnlyClosure132
                    && !enclosingBlock.HasImmutableLiteralOnlyClosure132 && !consBlock.HasImmutableLiteralOnlyClosure132
                    && pureBlock.HasImmutableReadonlyCaptureClosure138
                    && !tokenBlock.HasImmutableReadonlyCaptureClosure138 && !boxedBlock.HasImmutableReadonlyCaptureClosure138
                    && !enclosingBlock.HasImmutableReadonlyCaptureClosure138 && !consBlock.HasImmutableReadonlyCaptureClosure138);
                words = tokenBlock.AllocationWords526 + pureBlock.AllocationWords526
                    + enclosingBlock.AllocationWords526 + consBlock.AllocationWords526 + boxedBlock.AllocationWords526;
            }

            // A shaped ordinary immutable tuple is not a mutable-token writer
            // capability, even if trusted test code constructs a token view.
            SequenceTraceToken ordinaryView = SequenceTraceToken.FromPhysical(pure);
            bool rejected = false;
            try
            {
                heap.RewriteSequenceToken(pure, ordinaryView, ordinaryView with { Sender = context.Self });
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Require(rejected && pure.Items[SequenceTraceRootNames.Sender] is Atom);
            SequenceTraceToken boxedView = SequenceTraceToken.FromPhysical(boxedShape);
            bool boxedRejected = false;
            ProcessTermHeapSnapshot506 boxedBefore = heap.Snapshot;
            try
            {
                heap.RewriteSequenceToken(boxedShape, boxedView, boxedView with { Sender = context.Self });
            }
            catch (InvalidOperationException)
            {
                boxedRejected = true;
            }
            Require(boxedRejected && boxedShape.Items[SequenceTraceRootNames.Sender] is Atom
                && heap.Snapshot == boxedBefore);
            ProcessTermHeapSnapshot506 invalidBefore = heap.Snapshot;
            bool invalidRejected = false;
            try
            {
                roots.CreateMutableSequenceTokenInto550(roots.Reserve(), initial with { Sender = Nil.Value });
            }
            catch (InvalidOperationException)
            {
                invalidRejected = true;
            }
            Require(invalidRejected && heap.Snapshot == invalidBefore);
            ProcessTermHeapSnapshot506 before = heap.Snapshot;
            lock (runtimeGate)
                runtime.SequenceTracing.SentPrepared(
                    context,
                    context.Self,
                    Nil.Value,
                    null
                );
            SequenceTraceToken actual = context.SequentialTrace.Token!;
            Require(ReferenceEquals(actual.PhysicalTuple, token) && ReferenceEquals(actual.Sender, context.Self)
                && heap.Snapshot == before);
            lock (heap.Gate)
                Require(!MailboxBackingProbe.Storage(token)!.HasImmutablePlainClosure550
                    && !MailboxBackingProbe.Storage(enclosing)!.HasImmutablePlainClosure550
                    && !MailboxBackingProbe.Storage(cons)!.HasImmutablePlainClosure550);
            Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
            lock (runtimeGate) context.SequentialTrace.Token = null;
            // Only actual enclosing container roots now keep the token alive.
            Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0
                && ReferenceEquals(token.Items[SequenceTraceRootNames.Sender], context.Self));
            roots.Dispose();
            Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == words);
        }
        finally
        {
            try
            {
                lock (runtimeGate) context.SequentialTrace.Token = null;
            }
            finally
            {
                try
                {
                    pending?.Dispose();
                }
                finally
                {
                    roots.Dispose();
                }
            }
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(MutableSequenceToken550CaseNames.Failure);
    }
}
