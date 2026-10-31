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
