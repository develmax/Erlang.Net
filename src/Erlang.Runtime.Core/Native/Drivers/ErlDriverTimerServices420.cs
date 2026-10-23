using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverTimerServices420
{
    private readonly object gate = new();
    private readonly Dictionary<nint, ErlDriverTimer420> timers = new();
    private readonly List<ErlDriverTimer420> retiring = new();
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverPortJobs420 jobs;
    private readonly ErlDriverServiceFault420 fault;
    private bool closing;

    internal ErlDriverTimerServices420(Func<nint, ErlDriverPortReadLease420?> acquire, ErlDriverPortJobs420 jobs, ErlDriverServiceFault420 fault)
    {
        this.acquire = acquire;
        this.jobs = jobs;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_set_timer)] = new ErlDriverCallbackTypes420.driver_set_timer(Set),
            [nameof(ErlDriverCallbackTypes420.driver_cancel_timer)] = new ErlDriverCallbackTypes420.driver_cancel_timer(Cancel),
            [nameof(ErlDriverCallbackTypes420.driver_read_timer)] = new ErlDriverCallbackTypes420.driver_read_timer(Read)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private int Set(nint port, uint milliseconds)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return -1;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, this);
                if (!timers.TryGetValue(port, out var timer))
                {
                    timer = new(lease.Port, jobs, fault);
                    try
                    {
                        timers.Add(port, timer);
                    }
                    catch
                    {
                        timer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        throw;
                    }
                }
                timer.Set(milliseconds);
            }

            return 0;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int Cancel(nint port)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return -1;
            lock (gate)
                if (timers.TryGetValue(port, out var timer)) timer.Cancel();

            return 0;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int Read(nint port, nint remaining)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return -1;
            if (remaining == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            uint value;
            lock (gate) value = timers.TryGetValue(port, out var timer) ? timer.Read() : 0;
            Marshal.WriteInt32(remaining, unchecked((int)value));

            return 0;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    internal async Task RemovePortAsync(nint port)
    {
        ErlDriverTimer420? timer;
        lock (gate)
        {
            if (!timers.TryGetValue(port, out timer)) return;
            retiring.Add(timer);
            timers.Remove(port);
        }
        await timer.DisposeAsync().ConfigureAwait(false);
    }

    internal async Task CloseAsync()
    {
        ErlDriverTimer420[] owned;
        lock (gate)
        {
            closing = true;
            owned = timers.Values.Concat(retiring).Distinct().ToArray();
            timers.Clear();
        }
        var failures = new List<Exception>();
        foreach (var timer in owned)
        {
            try
            {
                await timer.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
