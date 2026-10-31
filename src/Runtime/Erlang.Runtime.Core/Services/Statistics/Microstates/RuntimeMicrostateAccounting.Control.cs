namespace Erlang;

internal sealed partial class RuntimeMicrostateAccounting
{
    internal RuntimeMicrostateRequest CreateRequest(RuntimeMicrostateAction action, int normalCapacity) => new(this, action, normalCapacity);

    private RuntimeMicrostateRecord Caller()
    {
        RuntimeMicrostateRecord? record = RuntimeMicrostateExecution.Current?.Record;
        if (record is null || !record.Managed || !ReferenceEquals(record.Owner, this)
            || !ReferenceEquals(record.PhysicalThread, RuntimeMicrostateThreadIdentity.Current))
            throw new InvalidOperationException(RuntimeNormalObservationNames.Requester);

        return record;
    }

    internal bool PrepareRequest(RuntimeMicrostateRequest request)
    {
        RuntimeMicrostateRecord caller = Caller();
        lock (gate)
        {
            RequireProvider();
            // Complete all preparation before the source BIF requested exchange.
            request.Unmanaged = records.Values.Where(record => !record.Managed).ToArray();
            request.ReserveUnmanaged(request.Unmanaged.Length);
            if (request.Action is RuntimeMicrostateAction.Enable or RuntimeMicrostateAction.Disable)
            {
                bool enable = request.Action == RuntimeMicrostateAction.Enable;
                request.Previous = Interlocked.Exchange(ref requested, enable ? 1 : 0) != 0;
                request.NoOp = caller.ManagedAttached == enable;
            }

            return !request.NoOp;
        }
    }

    internal void ApplyNormal(RuntimeMicrostateRequest request) => _ = ApplyNormalSnapshot(request);

    internal RuntimeMicrostateSnapshot? ApplyNormalSnapshot(RuntimeMicrostateRequest request)
    {
        RuntimeMicrostateSnapshot? result = null;
        RuntimeMicrostateRecord record = Caller();
        lock (gate)
        {
            switch (request.Action)
            {
                case RuntimeMicrostateAction.Enable when !record.ManagedAttached:
                    record.LastCounter = ErlangClockUnits413.PerformanceCounter;
                    record.State = RuntimeMicrostate.Other;
                    record.ManagedAttached = true;
                    break;
                case RuntimeMicrostateAction.Disable when record.ManagedAttached:
                    // Pinned managed disable clears TSD without a synthetic flush.
                    record.ManagedAttached = false;
                    break;
                case RuntimeMicrostateAction.Reset:
                    Array.Clear(record.Counters);
                    break;
                case RuntimeMicrostateAction.Gather:
                    // Pinned normal gather does NOT advance perf/state.
                    result = Snapshot(record);
                    request.Add(result);
                    break;
            }
        }

        return result;
    }

    internal void ApplyUnmanaged(RuntimeMicrostateRequest request, RuntimeMicrostateRecord record)
        => _ = ApplyUnmanagedSnapshot(request, record);

    internal RuntimeMicrostateSnapshot? ApplyUnmanagedSnapshot(RuntimeMicrostateRequest request, RuntimeMicrostateRecord record)
    {
        if (request.Action == RuntimeMicrostateAction.Gather)
        {
            lock (gate)
            {
                if (!record.LastCounter.IsZero)
                    Flush(record, ErlangClockUnits413.PerformanceCounter);
            }
            RuntimeMicrostateSnapshot snapshot;
            // Pinned gather unlocks after update, then send_reply reacquires the
            // unmanaged mutex for the actual snapshot. Preserve that window.
            lock (gate) snapshot = Snapshot(record);
            request.Add(snapshot);

            return snapshot;
        }
        lock (gate)
        {
            switch (request.Action)
            {
                case RuntimeMicrostateAction.Enable:
                    record.LastCounter = ErlangClockUnits413.PerformanceCounter;
                    // Exact source request initializes unmanaged state to Sleep.
                    // This does not create a thread or claim a missing idle row.
                    record.State = RuntimeMicrostate.Sleep;
                    break;
                case RuntimeMicrostateAction.Disable:
                    // Source does this unconditionally, including concurrent
                    // opposite requests; do not silently add a perf!=0 guard.
                    Flush(record, ErlangClockUnits413.PerformanceCounter);
                    record.LastCounter = 0;
                    break;
                case RuntimeMicrostateAction.Reset:
                    Array.Clear(record.Counters);
                    break;
            }
        }

        return null;
    }

    private static RuntimeMicrostateSnapshot Snapshot(RuntimeMicrostateRecord record)
        => new(record.Type, record.Identity, (System.Numerics.BigInteger[])record.Counters.Clone());

    internal void FinishRequest(RuntimeMicrostateRequest request)
    {
        if (request.Action == RuntimeMicrostateAction.Enable)
        {
            Volatile.Write(ref instrumentationEnabled, 1);
            RuntimeMicrostateExecution.RefreshCurrent(this);
        }
        else if (request.Action == RuntimeMicrostateAction.Disable)
            Volatile.Write(ref instrumentationEnabled, 0);
        else if (request.Action == RuntimeMicrostateAction.Reset)
            request.Previous = Volatile.Read(ref requested) != 0;
    }
}
