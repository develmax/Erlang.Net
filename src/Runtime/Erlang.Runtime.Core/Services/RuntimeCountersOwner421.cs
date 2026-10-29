namespace Erlang;

internal sealed partial class RuntimeCountersOwner421
{
    private readonly object gate = new();
    private readonly Dictionary<CountersArray421, LinkedListNode<CountersArray421>> arrays = new();
    private readonly LinkedList<CountersArray421> order = new();
    private int active;
    private bool closed;
    private bool draining596;
    private bool drained;
    private LinkedListNode<CountersArray421>? retirementCursor590;
    private long created, freed;
    private ulong bytes;
    internal RuntimeCountersReport421 Report
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

    internal CountersInvocation421 Enter()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var invocation = new CountersInvocation421(this);
            active = checked(active + 1);

            return invocation;
        }
    }

    internal void Leave()
    {
        lock (gate)
        {
            if (--active < 0) throw new InvalidOperationException(CountersNames421.Underflow);
            if (active == 0) Monitor.PulseAll(gate);
        }
    }

    internal CountersArray421 Create(ulong length, int lanes)
    {
        // Caller already owns a full invocation lease, so runtime Close joins allocation/publication rollback too.
        var array = new CountersArray421(length, lanes);
        try
        {
            var link = new LinkedListNode<CountersArray421>(array);
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

    internal void Retire(CountersArray421 array)
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
                array.ForgetReference596();
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

    internal void StopAdmission596()
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
            if (draining596)
            {
                while (!drained) Monitor.Wait(gate);

                return;
            }
            closed = true;
            draining596 = true;
            while (active != 0 || retirementWorkers596 != 0) Monitor.Wait(gate);
        }
        try
        {
            lock (gate)
                retirementCursor590 = order.First;
            while (true)
            {
                CountersArray421 array;
                lock (gate)
                {
                    LinkedListNode<CountersArray421>? current = retirementCursor590;
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
        if (retirementFailure596 is { } retirementFailure)
            failures.Add(retirementFailure);
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
