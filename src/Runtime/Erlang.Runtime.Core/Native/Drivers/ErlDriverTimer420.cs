using System.Diagnostics;

namespace Erlang;

internal sealed class ErlDriverTimer420 : IAsyncDisposable
{
    private const long MillisecondsPerSecond = 1000;
    private const long MaximumTimerChunk = uint.MaxValue - 1L;
    private readonly object gate = new();
    private readonly Timer timer;
    private readonly ErlDriverPort420 port;
    private readonly ErlDriverPortJobs420 jobs;
    private readonly ErlDriverServiceFault420 fault;
    private ulong generation;
    private long? deadline;
    private bool closed;
    private Task? close;

    internal ErlDriverTimer420(ErlDriverPort420 port, ErlDriverPortJobs420 jobs, ErlDriverServiceFault420 fault)
    {
        this.port = port;
        this.jobs = jobs;
        this.fault = fault;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            timer = new(
                Fire,
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan
            );
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    private static long Remaining(long target)
    {
        long now = Stopwatch.GetTimestamp();
        if (target <= now) return 0;

        return checked((long)(((Int128)(target - now) * MillisecondsPerSecond + Stopwatch.Frequency - 1) / Stopwatch.Frequency));
    }

    internal void Set(uint milliseconds)
    {
        long target = checked(Stopwatch.GetTimestamp() + checked((long)(((Int128)milliseconds * Stopwatch.Frequency
            + MillisecondsPerSecond - 1) / MillisecondsPerSecond)));
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            ulong next = checked(generation + 1);
            if (!timer.Change(TimeSpan.FromMilliseconds(Math.Min((long)milliseconds, MaximumTimerChunk)), Timeout.InfiniteTimeSpan))
                throw new ObjectDisposedException(nameof(ErlDriverTimer420));
            generation = next;
            deadline = target;
        }
    }

    internal void Cancel()
    {
        lock (gate)
        {
            if (closed) return;
            ulong next = checked(generation + 1);
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            generation = next;
            deadline = null;
        }
    }

    internal uint Read()
    {
        lock (gate)
        {
            if (closed || deadline is not { } target) return 0;

            return checked((uint)Remaining(target));
        }
    }

    private bool Admit(ulong expected)
    {
        lock (gate) return !closed && generation == expected;
    }

    private void Fire(object? state)
    {
        try
        {
            ulong expected;
            lock (gate)
            {
                if (closed || deadline is not { } target) return;
                long remaining = Remaining(target);
                if (remaining > 0)
                {
                    timer.Change(TimeSpan.FromMilliseconds(Math.Min(remaining, MaximumTimerChunk)), Timeout.InfiniteTimeSpan);

                    return;
                }
                expected = generation;
                deadline = null;
            }
            _ = jobs.Post(port.Identity, ErlDriverPortTaskKind420.Timeout,
                scheduler => port.Timeout(scheduler, () => Admit(expected)));
        }
        catch (Exception error)
        {
            fault.Record(error);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            if (close is not null) return new(close);
            closed = true;
            deadline = null;
            close = timer.DisposeAsync().AsTask();

            return new(close);
        }
    }
}
