namespace Erlang;

internal sealed class ErlDriverPdlServices420
{
    private readonly object gate = new();
    private readonly Dictionary<nint, ErlDriverPdl420> locks = new();
    private readonly Dictionary<nint, ErlDriverPdl420> ports = new();
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverServiceFault420 fault;
    private nint next;

    internal ErlDriverPdlServices420(Func<nint, ErlDriverPortReadLease420?> acquire, ErlDriverServiceFault420 fault)
    {
        this.acquire = acquire;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_pdl_create)] = new ErlDriverCallbackTypes420.driver_pdl_create(Create),
            [nameof(ErlDriverCallbackTypes420.driver_pdl_lock)] = new ErlDriverCallbackTypes420.driver_pdl_lock(Lock),
            [nameof(ErlDriverCallbackTypes420.driver_pdl_unlock)] = new ErlDriverCallbackTypes420.driver_pdl_unlock(Unlock),
            [nameof(ErlDriverCallbackTypes420.driver_pdl_get_refc)] = new ErlDriverCallbackTypes420.driver_pdl_get_refc(Count),
            [nameof(ErlDriverCallbackTypes420.driver_pdl_inc_refc)] = new ErlDriverCallbackTypes420.driver_pdl_inc_refc(Retain),
            [nameof(ErlDriverCallbackTypes420.driver_pdl_dec_refc)] = new ErlDriverCallbackTypes420.driver_pdl_dec_refc(Release)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private ErlDriverPdl420 Resolve(nint handle)
    {
        lock (gate)
        {
            if (!locks.TryGetValue(handle, out var value))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

            return value;
        }
    }

    private nint Create(nint port)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return nint.Zero;
            lock (gate)
            {
                if (ports.ContainsKey(port)) return nint.Zero;
                nint identity = checked(next + 1);
                var value = new ErlDriverPdl420(identity, lease.Port, Destroy);
                locks.Add(identity, value);
                try
                {
                    ports.Add(port, value);
                }
                catch
                {
                    locks.Remove(identity);
                    throw;
                }
                next = identity;

                return identity;
            }
        }
        catch (Exception error)
        {
            fault.Record(error);

            return nint.Zero;
        }
    }

    private void Destroy(ErlDriverPdl420 value)
    {
        lock (gate)
        {
            if (!locks.TryGetValue(value.Identity, out var current) || !ReferenceEquals(value, current))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            locks.Remove(value.Identity);
        }
    }

    internal void ReleasePort(nint port)
    {
        ErlDriverPdl420? value;
        lock (gate)
        {
            if (!ports.Remove(port, out value)) return;
        }
        value.ReleasePort();
    }

    private void Lock(nint handle)
    {
        try
        {
            Resolve(handle).Lock();
        }
        catch (Exception error)
        {
            fault.Record(error);
        }
    }

    private void Unlock(nint handle)
    {
        // Unlock remains available after port admission closes; it must not
        // acquire a now-closed port read lease while close waits for this lock.
        try
        {
            Resolve(handle).Unlock();
        }
        catch (Exception error)
        {
            fault.Record(error);
        }
    }

    private nint Count(nint handle)
    {
        try
        {
            return checked((nint)Resolve(handle).ReferenceCount);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private nint Retain(nint handle)
    {
        try
        {
            return checked((nint)Resolve(handle).Retain());
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private nint Release(nint handle)
    {
        try
        {
            return checked((nint)Resolve(handle).Release());
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    internal ErlDriverPdl420? RetainForAsync(nint port)
    {
        lock (gate)
        {
            if (!ports.TryGetValue(port, out var value)) return null;
            value.Retain();

            return value;
        }
    }

    internal ErlDriverPdl420? LockForMonitor(nint port)
    {
        var value = RetainForAsync(port);
        // Never wait for queueGate while holding the registry gate: final
        // Unlock can destroy the last PDL reference and remove its registry row.
        value?.LockRetained();

        return value;
    }

    internal void RequireDrained()
    {
        lock (gate)
            if (locks.Count != 0 || ports.Count != 0)
                throw new InvalidOperationException(ErlDriverHostNames420.Closed);
    }
}
