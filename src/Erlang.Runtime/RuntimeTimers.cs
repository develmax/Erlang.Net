using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimeTimers(ProcessRuntime runtime, object gate) : IDisposable
{
    private sealed class Entry(long milliseconds, Term destination, Term message)
    {
        public long Milliseconds { get; } = milliseconds;
        public long Started { get; } = Stopwatch.GetTimestamp();
        public Term Destination { get; } = destination;
        public Term Message { get; } = message;
        public Timer Timer { get; set; } = null!;

        public long Remaining => Math.Max(0, Milliseconds - Stopwatch.GetElapsedTime(Started).Ticks / TimeSpan.TicksPerMillisecond);
    }

    private readonly Dictionary<ReferenceTerm, Entry> timers = new();
    private bool disposed;

    public Term Start(
        ReferenceTerm reference,
        Term duration,
        Term destination,
        Term message,
        bool wrap,
        bool absolute = false
    )
    {
        var time = (Integer)ErlangTimerDeadline.Delay(duration, absolute);
        if (destination is not Atom && destination is not Pid { Creation: 0 } || destination is Pid pid && pid.Node != runtime.Node)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (destination is Pid target && !runtime.IsAlive(target))
                return reference;
            var entry = new Entry((long)time.Value, destination, wrap ? Term.Tuple(Term.A(TimerMessageAtoms.Timeout), reference, message) : message);
            entry.Timer = new Timer(
                _ => Fire(reference),
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );
            timers.Add(reference, entry);
            entry.Timer.Change((int)Math.Min(entry.Remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);
        }

        return reference;
    }

    public Term Access(Term term, bool cancel)
    {
        if (term is not ReferenceTerm reference)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            if (!timers.TryGetValue(reference, out var entry))
                return Term.A(ErlangBooleanAtoms.False);
            long remaining = entry.Remaining;
            if (cancel)
            {
                timers.Remove(reference);
                entry.Timer.Dispose();
            }

            return Term.I(remaining);
        }
    }

    private void Fire(ReferenceTerm reference)
    {
        lock (gate)
        {
            if (!timers.TryGetValue(reference, out var entry))
                return;
            long remaining = entry.Remaining;
            if (remaining > 0)
            {
                entry.Timer.Change((int)Math.Min(remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);

                return;
            }
            timers.Remove(reference);
            entry.Timer.Dispose();
            Term destination = entry.Destination is Atom name ? runtime.WhereIs(name.Name) : entry.Destination;
            if (destination is Pid target)
                runtime.Send(target, entry.Message);
        }
    }

    public void RecipientTerminated(Pid pid)
    {
        lock (gate)
            foreach (var pair in timers.Where(pair => pair.Value.Destination.Equals(pid)).ToArray())
            {
                timers.Remove(pair.Key);
                pair.Value.Timer.Dispose();
            }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            foreach (var timer in timers.Values)
                timer.Timer.Dispose();
            timers.Clear();
        }
    }
}
