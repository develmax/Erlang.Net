using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimeTimers(ProcessRuntime runtime, object gate) : IDisposable
{
    private sealed class Entry(long milliseconds, Term destination, Term message)
    {
        public long Milliseconds { get; set; } = milliseconds;
        public long Started { get; set; } = Stopwatch.GetTimestamp();
        public bool Paused { get; set; }
        public Term Destination { get; } = destination;
        public Term Message { get; } = message;
        public Timer Timer { get; set; } = null!;

        public long Remaining => Paused ? Milliseconds : Math.Max(0, Milliseconds - Stopwatch.GetElapsedTime(Started).Ticks / TimeSpan.TicksPerMillisecond);
    }

    private readonly Dictionary<ReferenceTerm, Entry> timers = new();
    private readonly HashSet<Task> retiringCallbacks = new();
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
            if (destination is Pid recipient && runtime.Suspensions.IsActive(recipient))
                entry.Paused = true;
            else
                Schedule(entry);
        }

        return reference;
    }

    public Term Access(Term term, bool cancel)
    {
        if (term is not ReferenceTerm reference)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            if (!timers.TryGetValue(reference, out var entry) || entry.Paused)
                return Term.A(ErlangBooleanAtoms.False);
            long remaining = entry.Remaining;
            if (cancel)
            {
                timers.Remove(reference);
                Retire(entry.Timer);
            }

            return Term.I(remaining);
        }
    }

    private void Fire(ReferenceTerm reference)
    {
        lock (gate)
        {
            if (!timers.TryGetValue(reference, out var entry) || entry.Paused)
                return;
            long remaining = entry.Remaining;
            if (remaining > 0)
            {
                entry.Timer.Change((int)Math.Min(remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);

                return;
            }
            timers.Remove(reference);
            Retire(entry.Timer);
            Term destination = entry.Destination is Atom name ? runtime.WhereIs(name.Name) : entry.Destination;
            if (destination is Pid target)
                runtime.SendSystem(target, entry.Message, Term.A(ProcessTraceNames.ClockService));
        }
    }

    public void PauseRecipient(Pid pid)
    {
        lock (gate)
            foreach (Entry entry in timers.Values.Where(entry => entry.Destination.Equals(pid) && !entry.Paused))
            {
                entry.Milliseconds = entry.Remaining;
                entry.Paused = true;
                entry.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
    }

    public void ResumeRecipient(Pid pid)
    {
        lock (gate)
            foreach (var pair in timers.Where(pair => pair.Value.Destination.Equals(pid) && pair.Value.Paused).ToArray())
            {
                Entry entry = pair.Value;
                timers.Remove(pair.Key);
                Retire(entry.Timer);
                ReferenceTerm privateReference = runtime.MakeReference();
                entry.Timer = new Timer(
                    _ => Fire(privateReference),
                    null,
                    Timeout.Infinite,
                    Timeout.Infinite
                );
                entry.Started = Stopwatch.GetTimestamp();
                entry.Paused = false;
                timers.Add(privateReference, entry);
                Schedule(entry);
            }
    }

    private static void Schedule(Entry entry) => entry.Timer.Change((int)Math.Min(entry.Remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);

    public void RecipientTerminated(Pid pid)
    {
        lock (gate)
            foreach (var pair in timers.Where(pair => pair.Value.Destination.Equals(pid)).ToArray())
            {
                timers.Remove(pair.Key);
                Retire(pair.Value.Timer);
            }
    }

    private void Retire(Timer timer)
    {
        Task completion = timer.DisposeAsync().AsTask();
        if (completion.IsCompletedSuccessfully)
            return;
        retiringCallbacks.Add(completion);
        _ = completion.ContinueWith(
            completed =>
        {
            lock (gate)
                if (completed.IsCompletedSuccessfully)
                    retiringCallbacks.Remove(completed);
        },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    internal Task StopForHaltAsync()
    {
        lock (gate)
        {
            disposed = true;
            foreach (Entry entry in timers.Values)
                Retire(entry.Timer);
            timers.Clear();

            return Task.WhenAll(retiringCallbacks.ToArray());
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            foreach (var timer in timers.Values)
                Retire(timer.Timer);
            timers.Clear();
        }
    }
}
