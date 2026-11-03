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
    private LinkedListNode<AtomicsArray413>? retirementCursor590;
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
        // Physical Close may report a callback error AFTER its finally freed
        // native memory. The identity must still retire in that case.
        Exception? closeFailure = null;
        Exception? referenceFailure = null;
        try
        {
            array.Close();
        }
        catch (Exception failure)
        {
            closeFailure = failure;
        }
        if (!array.IsAllocated434)
        {
            try
            {
                // No owner/array gate is held across reference-registry cleanup.
                array.ForgetReference586();
            }
            catch (Exception failure)
            {
                referenceFailure = failure;
            }
            if (referenceFailure is null)
            {
                lock (gate)
                {
                    if (arrays.Remove(array, out var link))
                    {
                        // Direct Retire can remove the next node while Close
                        // is physically retiring another allocation.
                        if (ReferenceEquals(retirementCursor590, link))
                            retirementCursor590 = link.Next;
                        order.Remove(link);
                        bytes -= array.AllocatedBytes;
                        freed++;
                    }
                }
            }
        }
        // Keep unresolved arrays registered; never count an attempted free as
        // success. Aggregate only after physical/reference cleanup was attempted.
        if (closeFailure is not null && referenceFailure is not null)
            throw new AggregateException(closeFailure, referenceFailure);
        if (closeFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(closeFailure).Throw();
        if (referenceFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(referenceFailure).Throw();
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
            while (active != 0 || retirementWorkers586 != 0) Monitor.Wait(gate);
        }
        try
        {
            lock (gate)
                retirementCursor590 = order.First;
            while (true)
            {
                AtomicsArray413 array;
                lock (gate)
                {
                    LinkedListNode<AtomicsArray413>? current = retirementCursor590;
                    if (current is null)
                        break;
                    retirementCursor590 = current.Next;
                    array = current.Value;
                }
                try
                {
                    Retire(array);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                    // Retire owns actual-free/reference accounting. Continue
                    // once per node; unresolved ownership stays registered.
                }
            }
        }
        finally
        {
            lock (gate)
            {
                retirementCursor590 = null;
                drained = true;
                Monitor.PulseAll(gate);
            }
        }
        if (retirementFailure586 is { } retirementFailure)
            failures.Add(retirementFailure);
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
