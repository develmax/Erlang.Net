namespace Erlang;

// One instance per logical VM. No static default shared by .NET hosts/VMs.
// This policy object is deliberately not wired to public system_flag while
// complete guest backing and all process-root domains remain unavailable.
internal sealed class RuntimeResourceDefaults508(PinnedHeapSizeClasses508 classes, ProcessResourcePolicy508 initial)
{
    private readonly object gate = new();
    private ProcessResourcePolicy508 current = initial;

    internal ProcessResourcePolicy508 Snapshot
    {
        get
        {
            lock (gate) return current;
        }
    }

    internal ProcessResourcePolicy508 ExchangeMinimumHeap(Term value)
    {
        ulong next = classes.Next(classes.NonnegativeSmall(value));
        lock (gate)
        {
            var previous = current;
            current = current with { MinimumHeapWords = next };

            return previous;
        }
    }

    internal ProcessResourcePolicy508 ExchangeMinimumBinaryHeap(Term value)
    {
        ulong next = classes.Next(classes.NonnegativeSmall(value));
        lock (gate)
        {
            var previous = current;
            current = current with { MinimumBinaryHeapWords = next };

            return previous;
        }
    }

    internal ProcessResourcePolicy508 ExchangeFullSweep(Term value)
    {
        ushort next = ProcessResourcePolicyParser508.SystemSweep(classes, value);
        lock (gate)
        {
            var previous = current;
            current = current with { FullSweepAfter = next };

            return previous;
        }
    }
}
