namespace Erlang;

// Queue operations are serialized by DirtyNativeOwner419.gate. All workers in
// a kind consume the same semaphore, so a held member cannot trap another job.
internal sealed class DirtyNativeQueue522
{
    private int members;
    internal LinkedList<DirtyNativeInvocation419> Items { get; } = new();
    internal SemaphoreSlim Signal { get; } = new(0);

    internal void AddMember() => Interlocked.Increment(ref members);

    internal void RemoveMember()
    {
        if (Interlocked.Decrement(ref members) == 0)
            Signal.Dispose();
    }
}
