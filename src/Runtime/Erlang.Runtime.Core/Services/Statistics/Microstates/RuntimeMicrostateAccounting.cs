using System.Numerics;

namespace Erlang;

internal sealed partial class RuntimeMicrostateAccounting
{
    private readonly object gate = new();
    private readonly Dictionary<(RuntimeMicrostateThreadIdentity Thread, string Type), RuntimeMicrostateRecord> records = new();
    private readonly LinkedList<RuntimeMicrostateRecord> unmanagedRecords1171 = new();
    private readonly Dictionary<string, int> identities = new(StringComparer.Ordinal);
    private int requested, instrumentationEnabled;
    private Exception? unavailable;

    internal RuntimeMicrostateScope? Attach(string type, RuntimeMicrostate state)
        => AttachCore(type, state, null);

    internal RuntimeMicrostateScope? AttachConfiguredWorker(string type, int identity, RuntimeMicrostate state)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(identity);

        return AttachCore(type, state, identity);
    }

    internal RuntimeMicrostateScope? AttachManagedWorker989(string type, int identity, RuntimeMicrostate state)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(identity);

        return AttachClassifiedCore989(
            type,
            state,
            identity,
            true
        );
    }

    private RuntimeMicrostateScope? AttachCore(string type, RuntimeMicrostate state, int? configuredIdentity)
        => AttachClassifiedCore989(
            type,
            state,
            configuredIdentity,
            type == RuntimeMicrostateNames.Scheduler
        );

    private RuntimeMicrostateScope? AttachClassifiedCore989(
        string type,
        RuntimeMicrostate state,
        int? configuredIdentity,
        bool managed
    )
    {
        RuntimeMicrostateThreadIdentity thread;
        try
        {
            thread = RuntimeMicrostateThreadIdentity.Current;
        }
        catch (Exception error) when (error is PlatformNotSupportedException or DllNotFoundException
            or EntryPointNotFoundException or InvalidOperationException)
        {
            lock (gate) unavailable ??= error;

            return null;
        }

        return managed == true
            ? RuntimeMicrostateExecution.AttachManagedWorker989(
                this,
                type,
                state,
                thread,
                configuredIdentity
            )
            : RuntimeMicrostateExecution.AttachConfiguredWorker(
                this,
                type,
                state,
                thread,
                configuredIdentity
            );
    }

    private void RequireProvider()
    {
        if (unavailable is not null)
            throw new NotSupportedException(RuntimeMicrostateNames.Unavailable, unavailable);
    }

    internal RuntimeMicrostateRecord Register(string type, RuntimeMicrostateThreadIdentity thread)
        => RegisterWorker(type, thread, null);

    internal RuntimeMicrostateRecord RegisterWorker(string type, RuntimeMicrostateThreadIdentity thread, int? configuredIdentity)
        => RegisterClassifiedWorker989(
            type,
            thread,
            configuredIdentity,
            type == RuntimeMicrostateNames.Scheduler
        );

    internal RuntimeMicrostateRecord RegisterClassifiedWorker989(
        string type,
        RuntimeMicrostateThreadIdentity thread,
        int? configuredIdentity,
        bool managed
    )
    {
        lock (gate)
        {
            if (records.TryGetValue((thread, type), out var existing))
            {
                if (existing.Managed != managed
                    || configuredIdentity is { } expected && existing.Identity != expected)
                    throw new InvalidOperationException(RuntimeMicrostateNames.ThreadMismatch);

                return existing;
            }
            int identity = configuredIdentity ?? checked(identities.GetValueOrDefault(type) + 1);
            foreach (var candidate in records.Values)
                if (candidate.Type == type && candidate.Identity == identity)
                    throw new InvalidOperationException(RuntimeMicrostateNames.ThreadMismatch);
            var record = new RuntimeMicrostateRecord(
                this,
                type,
                identity,
                thread,
                managed
            );
            LinkedListNode<RuntimeMicrostateRecord>? unmanagedNode = managed ? null : new(record);
            records.EnsureCapacity(records.Count + 1);
            identities.EnsureCapacity(identities.Count + 1);
            identities[type] = Math.Max(identities.GetValueOrDefault(type), identity);
            records.Add((thread, type), record);
            if (unmanagedNode is not null) unmanagedRecords1171.AddFirst(unmanagedNode);

            return record;
        }
    }

    internal void Resume(RuntimeMicrostateRecord record, RuntimeMicrostate state)
    {
        lock (gate)
        {
            if (record.Active) throw new InvalidOperationException(RuntimeMicrostateNames.ThreadMismatch);
            record.Active = true;
            if (EnabledFor(record))
            {
                record.LastCounter = ErlangClockUnits413.PerformanceCounter;
                record.State = state;
            }
            else if (!record.Managed && Volatile.Read(ref instrumentationEnabled) != 0)
            {
                // An unmanaged record with perf=0 still accepts its real state
                // transition, without inventing an accounting origin.
                record.State = state;
            }
        }
    }

    internal RuntimeMicrostate Pause(RuntimeMicrostateRecord record)
    {
        lock (gate)
        {
            if (EnabledFor(record)) Flush(record, ErlangClockUnits413.PerformanceCounter);
            record.Active = false;

            return record.State;
        }
    }

    internal RuntimeMicrostate Change(RuntimeMicrostateRecord record, RuntimeMicrostate state)
    {
        lock (gate)
        {
            RuntimeMicrostate previous = record.State;
            if (Volatile.Read(ref instrumentationEnabled) == 0) return previous;
            if (record.Managed && !record.ManagedAttached) return previous;
            if (!record.Managed && record.LastCounter.IsZero)
            {
                record.State = state;

                return previous;
            }
            if (previous != state)
            {
                Flush(record, ErlangClockUnits413.PerformanceCounter);
                record.State = state;
            }

            return previous;
        }
    }

    private bool EnabledFor(RuntimeMicrostateRecord record)
        => Volatile.Read(ref instrumentationEnabled) != 0
            && (record.Managed ? record.ManagedAttached : !record.LastCounter.IsZero);

    private static void Flush(RuntimeMicrostateRecord record, BigInteger now)
    {
        if (record.Active)
            record.Counters[(int)record.State] += now - record.LastCounter;
        record.LastCounter = now;
    }
}
