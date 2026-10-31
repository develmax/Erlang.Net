using System.Numerics;

namespace Erlang.NifNativeBackend;

public static class ImmediateConsClosureChecks
{
    public static async Task Physical()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
            {
                var heap = new ProcessTermHeap506(runtime);
                var foreign = new ProcessTermHeap506(runtime);
                try
                {
                    using var frame = heap.Enter();
                    using var other = foreign.Enter();
                    int slot = frame.Reserve();
                    Term list = Nil.Value;
                    for (int index = 0; index < ImmediateConsClosureCaseNames.Length; index++)
                        list = frame.CreateConsInto507(slot, Term.I(index), list);
                    Require(MailboxBackingProbe.Storage(list)!.HasImmediateConsClosure);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(Cons.Items(list).Count() == ImmediateConsClosureCaseNames.Length);
                    frame.SetLocalGraph507(slot, list);
                    int boxedSlot = frame.Reserve();
                    frame.SetPrepared(boxedSlot, new Integer(BigInteger.One << ImmediateConsClosureCaseNames.BoxedShift));
                    int complexSlot = frame.Reserve();
                    Term boxed = frame.CreateConsInto507(complexSlot, frame.Read(boxedSlot), list);
                    Require(!MailboxBackingProbe.Storage(boxed)!.HasImmediateConsClosure);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(Cons.Items(boxed).Count() == ImmediateConsClosureCaseNames.Length + 1);
                    int foreignSlot = other.Reserve();
                    Term foreignList = other.CreateConsInto507(foreignSlot, Term.I(1), Nil.Value);
                    Require(MailboxBackingProbe.Storage(foreignList)!.HasImmediateConsClosure);
                    RejectForeign(() => frame.SetLocalGraph507(slot, foreignList));
                    // A local block can be allocated with unchecked portable
                    // children by trusted low-level code. It must NOT receive a
                    // closure certificate and cannot hide a foreign descendant.
                    Term malformed;
                    lock (heap.Gate) malformed = heap.CreateCons(Term.Tuple(foreignList), Nil.Value);
                    Require(!MailboxBackingProbe.Storage(malformed)!.HasImmediateConsClosure);
                    RejectForeign(() => frame.SetLocalGraph507(slot, malformed));
                    frame.Clear(slot);
                    frame.Clear(boxedSlot);
                    frame.Clear(complexSlot);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    RejectStale(() => frame.SetLocalGraph507(slot, list));
                    GC.KeepAlive(list);
                    GC.KeepAlive(boxed);
                }
                finally
                {
                    await heap.RetireAsync().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                    await foreign.RetireAsync().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                }

                return Nil.Value;
            }
            );
        }
    }

    public static async Task NestedPhysical()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
            {
                var heap = new ProcessTermHeap506(runtime);
                var foreign = new ProcessTermHeap506(runtime);
                try
                {
                    using var frame = heap.Enter();
                    using var other = foreign.Enter();
                    int childSlot = frame.Reserve();
                    int parentSlot = frame.Reserve();
                    Term list = Nil.Value;
                    for (int index = 0; index < ImmediateConsClosureCaseNames.NestedLength; index++)
                    {
                        Term tuple = frame.CreateTupleInto507(childSlot, new Term[] { Term.I(index), list });
                        Require(MailboxBackingProbe.Storage(tuple)!.HasImmutablePlainClosure550);
                        list = frame.CreateConsInto507(parentSlot, tuple, list);
                    }
                    Require(MailboxBackingProbe.Storage(list)!.HasImmutablePlainClosure550);
                    frame.Clear(childSlot);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(Cons.Items(list).Count() == ImmediateConsClosureCaseNames.NestedLength);
                    int remaining = ImmediateConsClosureCaseNames.NestedLength;
                    for (Term cursor = list; cursor is Cons cell; cursor = cell.Tail)
                    {
                        var child = (TupleTerm)cell.Head;
                        Require(((Integer)child.Items[0]).Value == --remaining);
                        Require(ReferenceEquals(child.Items[1], cell.Tail));
                    }
                    Require(remaining == 0);
                    frame.SetLocalGraph507(parentSlot, list);
                    // Actual allocated tuple takes its own immutable child snapshot.
                    Term[] children = [list];
                    Term parent = frame.CreateTupleInto507(childSlot, children);
                    children[0] = new BitString(new byte[ImmediateConsClosureCaseNames.BinaryBytes]);
                    Require(ReferenceEquals(((TupleTerm)parent).Items[0], list));
                    Require(MailboxBackingProbe.Storage(parent)!.HasImmutablePlainClosure550);
                    Require(ReferenceEquals(DebugDirtyCopyFrame424.ReadChildren(parent)[0], list));
                    ProcessTermSequence506 parentBlock = MailboxBackingProbe.Storage(parent)!;
                    lock (heap.Gate) parentBlock.Slab.Cells.WriteTerms526(new Term[] { Term.I(ImmediateConsClosureCaseNames.CellMutationValue) }, parentBlock.Offset);
                    Require(((Integer)DebugDirtyCopyFrame424.ReadChildren(parent)[0]).Value == ImmediateConsClosureCaseNames.CellMutationValue);
                    int excludedSlot = frame.Reserve();
                    frame.SetPrepared(excludedSlot, new Integer(BigInteger.One << ImmediateConsClosureCaseNames.BoxedShift));
                    Term excluded = frame.CreateTupleInto507(parentSlot, new Term[] { frame.Read(excludedSlot), list });
                    Require(!MailboxBackingProbe.Storage(excluded)!.HasImmutablePlainClosure550);
                    frame.SetPrepared(excludedSlot, new BitString(new byte[ImmediateConsClosureCaseNames.BinaryBytes]));
                    excluded = frame.CreateTupleInto507(parentSlot, new Term[] { frame.Read(excludedSlot), list });
                    Require(!MailboxBackingProbe.Storage(excluded)!.HasImmutablePlainClosure550);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(((BitString)((TupleTerm)excluded).Items[0]).ToArray().Length == ImmediateConsClosureCaseNames.BinaryBytes);
                    int foreignSlot = other.Reserve();
                    Term foreignTuple = other.CreateTupleInto507(foreignSlot, new Term[] { Term.I(1) });
                    Require(MailboxBackingProbe.Storage(foreignTuple)!.HasImmutablePlainClosure550);
                    RejectForeign(() => frame.SetLocalGraph507(parentSlot, foreignTuple));
                    Term malformed;
                    lock (heap.Gate) malformed = heap.CreateTuple([foreignTuple]);
                    Require(!MailboxBackingProbe.Storage(malformed)!.HasImmutablePlainClosure550);
                    RejectForeign(() => frame.SetLocalGraph507(parentSlot, malformed));
                    Term portable;
                    lock (heap.Gate) portable = heap.CreateTuple([Term.Tuple(Term.I(1))]);
                    Require(!MailboxBackingProbe.Storage(portable)!.HasImmutablePlainClosure550);
                    frame.Clear(childSlot);
                    frame.Clear(parentSlot);
                    frame.Clear(excludedSlot);
                    _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    RejectStale(() => frame.SetLocalGraph507(parentSlot, list));
                    GC.KeepAlive(parent);
                    GC.KeepAlive(excluded);
                }
                finally
                {
                    await heap.RetireAsync().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                    await foreign.RetireAsync().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                }

                return Nil.Value;
            }
            );
        }
    }

    private static void RejectForeign(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException error) when (error.Message == ProcessTerm506Names.ForeignRoot)
        {
            return;
        }
        throw new InvalidOperationException(ImmediateConsClosureCaseNames.Failure);
    }

    private static void RejectStale(Action action)
    {
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(ImmediateConsClosureCaseNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ImmediateConsClosureCaseNames.Failure);
    }
}
