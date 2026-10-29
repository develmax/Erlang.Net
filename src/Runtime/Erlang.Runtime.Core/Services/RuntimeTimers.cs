using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimeTimers(ProcessRuntime runtime, object gate) : IDisposable
{
    private readonly Dictionary<ReferenceTerm, RuntimeTimerEntry628> timers = new();
    private readonly HashSet<Task> retiringCallbacks = new();
    private bool disposed;
    private RuntimeTimerCallbackPhase539? callbackPhase539;

    internal void AttachCallbackPhase539(RuntimeTimerCallbackPhase539 phase)
    {
        ArgumentNullException.ThrowIfNull(phase);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (callbackPhase539 is not null || timers.Count != 0 || retiringCallbacks.Count != 0)
                throw new InvalidOperationException(RuntimeTimerCallbackPhase539Names.AttachAfterAdmission);
            phase.Bind539(gate);
            callbackPhase539 = phase;
        }
    }

    public Term Start(
        ReferenceTerm reference,
        Term duration,
        Term destination,
        Term message,
        bool wrap,
        bool absolute = false
    )
    {
        using var atoms = new AtomNameScope(runtime.AtomNames);
        var time = (Integer)ErlangTimerDeadline.Delay(duration, absolute);
        long started = Stopwatch.GetTimestamp();
        if (destination is not Atom && destination is not Pid || destination is Pid pid && !runtime.IsCurrentLocal(pid))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term outgoing = wrap ? Term.Tuple(Term.A(TimerMessageAtoms.Timeout), reference, message) : message;
        RuntimePendingPayload628 payload = RuntimePendingPayload628.Prepare(
            runtime,
            Term.Tuple(reference, destination, outgoing)
        );
        bool transferred = false;
        Exception? primary = null;
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (destination is Pid target && !runtime.IsAlive(target))
                    return reference;
                var entry = new RuntimeTimerEntry628((long)time.Value, payload) { Started = started };
                ReferenceTerm storedReference = entry.Reference;
                entry.Timer = CreateTimer(storedReference, entry);
                try
                {
                    timers.Add(storedReference, entry);
                    transferred = true;
                    if (destination is Pid recipient && runtime.Suspensions.IsActive(recipient))
                        entry.Paused = true;
                    else
                        Schedule(entry);
                }
                catch
                {
                    if (timers.TryGetValue(storedReference, out var selected) && ReferenceEquals(selected, entry))
                        timers.Remove(storedReference);
                    // Once constructed, retire the real Timer and payload through
                    // one detached owner, including publication rollback.
                    transferred = true;
                    RetireEntry(entry);
                    throw;
                }
            }

            return reference;
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            if (!transferred)
                try
                {
                    payload.DisposeAsync().GetAwaiter().GetResult();
                }
                catch (Exception cleanup) when (primary is not null)
                {
                    throw new AggregateException(primary, cleanup);
                }
        }
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
                RetireEntry(entry);
            }

            return Term.I(remaining);
        }
    }

    private void Fire(ReferenceTerm reference, RuntimeTimerEntry628 entry)
    {
        RuntimeTimerCallbackPhase539? phase539 = null;
        try
        {
            using var atoms = new AtomNameScope(runtime.AtomNames);
            lock (gate)
            {
                if (!timers.TryGetValue(reference, out var selected) || !ReferenceEquals(selected, entry) || entry.Paused)
                    return;
                long remaining = entry.Remaining;
                if (remaining > 0)
                {
                    entry.Timer.Change((int)Math.Min(remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);

                    return;
                }
                timers.Remove(reference);
                // Register before leaving R. Shutdown joins this in-flight
                // callback AND the last payload-root retirement it produces.
                RetireEntry(entry);
                phase539 = callbackPhase539;
            }
            using var publication = MailboxPublicationScope593.Forward(runtime, entry.Payload.Graph);
            phase539?.WaitAfterClaim539();
            runtime.SendTimerSnapshot628(entry.Destination, entry.Message, publication);
        }
        catch (Exception error)
        {
            // ThreadPool Timer callbacks cannot return a faulted Task. Preserve
            // the actual error in the joined retirement rather than terminating
            // the host or silently discarding a failed native publication.
            lock (gate)
            {
                (entry.CallbackFailures ??= new()).Add(error);
                if (timers.TryGetValue(reference, out var selected) && ReferenceEquals(selected, entry))
                    timers.Remove(reference);
                RetireEntry(entry);
            }
        }
        finally
        {
            phase539?.SignalExited539();
        }
    }

    public void PauseRecipient(Pid pid)
    {
        lock (gate)
            foreach (RuntimeTimerEntry628 entry in timers.Values.Where(entry => entry.Destination.Equals(pid) && !entry.Paused))
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
                RuntimeTimerEntry628 entry = pair.Value;
                timers.Remove(pair.Key);
                ReferenceTerm? privateReference = null;
                try
                {
                    RetireTimer(entry.Timer);
                    privateReference = runtime.MakeReference();
                    entry.Timer = CreateTimer(privateReference, entry);
                    entry.Started = Stopwatch.GetTimestamp();
                    entry.Paused = false;
                    timers.Add(privateReference, entry);
                    Schedule(entry);
                }
                catch
                {
                    if (privateReference is not null && timers.TryGetValue(privateReference, out var selected)
                        && ReferenceEquals(selected, entry))
                        timers.Remove(privateReference);
                    RetireEntry(entry);
                    throw;
                }
            }
    }

    private Timer CreateTimer(ReferenceTerm reference, RuntimeTimerEntry628 entry)
    {
        // A system callback must not inherit an actor turn or a disposed Source
        // invocation through Timer's ExecutionContext capture.
        if (ExecutionContext.IsFlowSuppressed())
            return new Timer(
                _ => Fire(reference, entry),
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );
        using (ExecutionContext.SuppressFlow())
            return new Timer(
                _ => Fire(reference, entry),
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );
    }

    private static void Schedule(RuntimeTimerEntry628 entry)
        => entry.Timer.Change((int)Math.Min(entry.Remaining, TimerSchedulingLimits.MaximumCallbackDelayMilliseconds), Timeout.Infinite);

    public void RecipientTerminated(Pid pid)
    {
        lock (gate)
            foreach (var pair in timers.Where(pair => pair.Value.Destination.Equals(pid)).ToArray())
            {
                timers.Remove(pair.Key);
                RetireEntry(pair.Value);
            }
    }

    private void RetireTimer(Timer timer) => Track(timer.DisposeAsync().AsTask());

    private void RetireEntry(RuntimeTimerEntry628 entry)
    {
        if (entry.RetirementStarted)
            return;
        entry.RetirementStarted = true;
        Task callbacks = entry.Timer.DisposeAsync().AsTask();
        Task retirement;
        if (ExecutionContext.IsFlowSuppressed())
            retirement = CompleteEntry(entry, callbacks);
        else
            using (ExecutionContext.SuppressFlow())
                retirement = CompleteEntry(entry, callbacks);
        Track(retirement);
    }

    private async Task CompleteEntry(RuntimeTimerEntry628 entry, Task callbacks)
    {
        // Always detach from the invoking actor's scheduler and ExecutionContext.
        // A foreign runtime may be synchronously disposed by that same actor.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        using var atoms = new AtomNameScope(runtime.AtomNames);
        Exception? callbackWaitFailure = null, payloadFailure = null;
        try
        {
            await callbacks.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            callbackWaitFailure = error;
        }
        try
        {
            await entry.Payload.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            payloadFailure = error;
        }
        // Allocate aggregation only after every deterministic owner is settled.
        Exception[] failures = new[] { callbackWaitFailure, payloadFailure }.OfType<Exception>()
            .Concat(entry.CallbackFailures?.AsEnumerable() ?? Enumerable.Empty<Exception>()).ToArray();
        if (failures.Length != 0)
            throw new AggregateException(failures);
    }

    private void Track(Task completion)
    {
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
            Close();

            return Task.WhenAll(retiringCallbacks.ToArray());
        }
    }

    private void Close()
    {
        disposed = true;
        foreach (RuntimeTimerEntry628 entry in timers.Values)
            RetireEntry(entry);
        timers.Clear();
    }

    public void Dispose()
    {
        lock (gate)
            Close();
    }
}
