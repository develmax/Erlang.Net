using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverMonitorServices420
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ErlDriverMonitorRow420> rows = new();
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverPdlServices420 pdl;
    private readonly IErlDriverMonitorHost420 host;
    private readonly ErlDriverPortJobs420 jobs;
    private readonly ErlDriverTermTokens420 tokens;
    private readonly ErlDriverServiceFault420 fault;
    private ulong next;
    private bool closing;

    internal ErlDriverMonitorServices420(
        Func<nint, ErlDriverPortReadLease420?> acquire,
        ErlDriverPdlServices420 pdl,
        IErlDriverMonitorHost420 host,
        ErlDriverPortJobs420 jobs,
        ErlDriverTermTokens420 tokens,
        ErlDriverServiceFault420 fault
    )
    {
        this.acquire = acquire;
        this.pdl = pdl;
        this.host = host;
        this.jobs = jobs;
        this.tokens = tokens;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_monitor_process)] = new ErlDriverCallbackTypes420.driver_monitor_process(Register),
            [nameof(ErlDriverCallbackTypes420.driver_demonitor_process)] = new ErlDriverCallbackTypes420.driver_demonitor_process(Unregister),
            [nameof(ErlDriverCallbackTypes420.driver_get_monitored_process)] = new ErlDriverCallbackTypes420.driver_get_monitored_process(Get),
            [nameof(ErlDriverCallbackTypes420.driver_compare_monitors)] = new ErlDriverCallbackTypes420.driver_compare_monitors(Compare)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private ErlDriverPdl420? Enter(nint port)
    {
        var locked = pdl.LockForMonitor(port);
        if (locked is null && !ErlDriverThreadRole420.IsSchedulerThread)
            throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);

        return locked;
    }

    private int Register(nint port, nuint process, nint output)
    {
        ErlDriverMonitorRow420? row = null;
        try
        {
            using var lease = acquire(port);
            if (lease is null) return -1;
            var locked = Enter(port);
            try
            {
                if (lease.Port.Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.ProcessExit) is null) return -1;
                if (tokens.Resolve(process) is not Pid target) return 1;
                if (output == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(closing, this);
                    ulong attempt = checked(next + 1);
                    row = new(attempt, lease.Port, process);
                    rows.Add(attempt, row);
                    next = attempt;
                }
                // A reverse ProcessExited may arrive before this response. Its
                // row already exists and waits Ready before scheduling a callback.
                var result = host.Register(
                    lease.Port.Identity,
                    row.Attempt,
                    tokens.ActorIdentity(process),
                    target
                );
                lock (gate)
                {
                    if (result.Status == 0)
                    {
                        if (result.Prefix is null || result.Prefix.Length != ErlDriverMonitorLayout420.PrefixBytes)
                            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        row.Prefix = result.Prefix;
                        Marshal.Copy(
                            result.Prefix,
                            0,
                            output,
                            result.Prefix.Length
                        );
                    }
                    else
                    {
                        if (result.Status != 1) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        row.Removed = true;
                        rows.Remove(row.Attempt);
                    }
                }

                return result.Status;
            }
            finally
            {
                locked?.Unlock();
            }
        }
        catch (Exception error)
        {
            if (row is not null)
            {
                lock (gate)
                {
                    row.Removed = true;
                    rows.Remove(row.Attempt);
                }
                try
                {
                    host.Unregister(row.Port.Identity, row.Attempt);
                }
                catch (Exception cleanup)
                {
                    fault.Record(cleanup);
                }
            }
            fault.Record(error);

            return -1;
        }
        finally
        {
            row?.Ready.TrySetResult();
        }
    }

    private static byte[] Read(nint monitor)
    {
        if (monitor == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[ErlDriverMonitorLayout420.PrefixBytes];
        Marshal.Copy(
            monitor,
            bytes,
            0,
            bytes.Length
        );

        return bytes;
    }

    private ErlDriverMonitorRow420? Find(ErlDriverPort420 port, byte[] prefix)
    {
        lock (gate)
            return rows.Values.FirstOrDefault(row => ReferenceEquals(row.Port, port) && !row.Removed
                && row.Prefix is not null && prefix.AsSpan().SequenceEqual(row.Prefix));
    }

    private int Unregister(nint port, nint monitor)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return -1;
            var locked = Enter(port);
            try
            {
                var row = Find(lease.Port, Read(monitor));
                if (row is null) return 1;
                lock (gate)
                {
                    row.Removed = true;
                    rows.Remove(row.Attempt);
                }
                host.Unregister(row.Port.Identity, row.Attempt);

                return 0;
            }
            finally
            {
                locked?.Unlock();
            }
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private nuint Get(nint port, nint monitor)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return tokens.NilValue;
            var locked = Enter(port);
            try
            {
                return Find(lease.Port, Read(monitor))?.Process ?? tokens.NilValue;
            }
            finally
            {
                locked?.Unlock();
            }
        }
        catch (Exception error)
        {
            fault.Record(error);

            return tokens.NilValue;
        }
    }

    [DllImport(ErlDriverHostNativeNames420.WindowsCrt, EntryPoint = ErlDriverHostNativeNames420.CompareBytes, CallingConvention = CallingConvention.Cdecl)]
    private static extern int memcmp(nint left, nint right, nuint bytes);

    private int Compare(nint left, nint right)
    {
        try
        {
            if (left == nint.Zero || right == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

            return memcmp(left, right, ErlDriverMonitorLayout420.PrefixBytes);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return 0;
        }
    }

    internal async Task ProcessExited(ErlDriverPortIdentity420 port, ulong attempt)
    {
        ErlDriverMonitorRow420? row;
        lock (gate)
        {
            if (!rows.TryGetValue(attempt, out row) || row.Port.Identity != port || row.Exited) return;
            row.Exited = true;
        }
        await row.Ready.Task.ConfigureAwait(false);
        lock (gate)
            if (row.Removed || row.Prefix is null) return;
        try
        {
            await jobs.Post(
                port,
                ErlDriverPortTaskKind420.ProcessExit,
                scheduler => row.Port.ProcessExited(row.Prefix!, scheduler, () =>
                {
                    lock (gate) return !row.Removed;
                })
            ).ConfigureAwait(false);
        }
        finally
        {
            // Keep the origin queryable during process_exit, matching io.c.
            var locked = pdl.LockForMonitor(row.Port.NativePort);
            try
            {
                lock (gate)
                {
                    row.Removed = true;
                    rows.Remove(attempt);
                }
            }
            finally
            {
                locked?.Unlock();
            }
        }
    }

    internal void RemovePort(nint port)
    {
        ErlDriverMonitorRow420[] owned;
        lock (gate)
        {
            owned = rows.Values.Where(row => row.Port.NativePort == port).ToArray();
            foreach (var row in owned)
            {
                row.Removed = true;
                rows.Remove(row.Attempt);
            }
        }
        var failures = new List<Exception>();
        foreach (var row in owned)
        {
            try
            {
                host.Unregister(row.Port.Identity, row.Attempt);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            row.Ready.TrySetResult();
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    internal void Close()
    {
        nint[] ports;
        lock (gate)
        {
            closing = true;
            ports = rows.Values.Select(row => row.Port.NativePort).Distinct().ToArray();
        }
        var failures = new List<Exception>();
        foreach (var port in ports)
        {
            try
            {
                RemovePort(port);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
