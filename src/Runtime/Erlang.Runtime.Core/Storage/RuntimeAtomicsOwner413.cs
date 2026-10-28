namespace Erlang;

internal sealed partial class RuntimeAtomicsOwner413
{
    private readonly object gate = new();
    private readonly Dictionary<AtomicsArray413, LinkedListNode<AtomicsArray413>> arrays = new();
    private readonly LinkedList<AtomicsArray413> order = new();
    private int active;
    private bool closed;
    private bool draining586;
    private bool drained;
    private long created, freed;
    private ulong bytes;
    internal RuntimeAtomicsReport413 Report
    {
        get
        {
            lock (gate) return new(
                created,
                freed,
                arrays.Count,
                bytes,
                active,
                closed
            );
        }
    }

    internal AtomicsInvocation413 Enter()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var invocation = new AtomicsInvocation413(this);
            active = checked(active + 1);

            return invocation;
        }
    }

    internal void Leave()
    {
        lock (gate)
        {
            if (--active < 0) throw new InvalidOperationException(AtomicsNames413.Underflow);
            if (active == 0) Monitor.PulseAll(gate);
        }
    }

    internal AtomicsArray413 Create(ulong length, bool signed)
    {
        // Caller already owns a full invocation lease, so runtime Close joins allocation/publication rollback too.
        var array = new AtomicsArray413(length, signed);
        try
        {
            var link = new LinkedListNode<AtomicsArray413>(array);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);
                ulong next = checked(bytes + array.AllocatedBytes);
                arrays.Add(array, link);
                order.AddLast(link);
                bytes = next;
                created++;
            }

            return array;
        }
        catch
        {
            array.Close();
            throw;
        }
    }

    internal void Retire(AtomicsArray413 array)
    {
        // Keep the record in the owner set until its actual free has completed; concurrent runtime Close must join it.
        array.Close();
        lock (gate)
        {
            if (!arrays.Remove(array, out var link)) return;
            order.Remove(link);
            bytes -= array.AllocatedBytes;
            freed++;
        }
    }

    internal void StopAdmission586()
    {
        lock (gate)
            closed = true;
    }

    internal void Close()
    {
        // Allocate failure aggregation before publishing the closing state.
        // If allocation itself fails, no waiter can observe an unfinishable close transition.
        var failures = new List<Exception>();
        lock (gate)
        {
            if (draining586)
            {
                while (!drained) Monitor.Wait(gate);

                return;
            }
            closed = true;
            draining586 = true;
            while (active != 0) Monitor.Wait(gate);
        }
        try
        {
            while (true)
            {
                AtomicsArray413 array;
                lock (gate)
                {
                    if (order.First is null) break;
                    array = order.First.Value;
                }
                try
                {
                    Retire(array);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                    // Native memory Close has completed its finally barrier even when destruction reports failure.
                    lock (gate)
                        if (arrays.Remove(array, out var link))
                        {
                            order.Remove(link);
                            bytes -= array.AllocatedBytes;
                            freed++;
                        }
                }
            }
        }
        finally
        {
            lock (gate)
            {
                drained = true;
                Monitor.PulseAll(gate);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
