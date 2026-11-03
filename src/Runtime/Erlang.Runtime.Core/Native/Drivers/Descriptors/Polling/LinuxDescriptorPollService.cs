using System.Runtime.InteropServices;

namespace Erlang;

// One actual Linux poll thread per owning logical VM. No data
// transfers and no ownership of supplied raw FDs. Start is explicit so caller
// can retain this owner before a partially successful Thread.Start attempt.
internal sealed class LinuxDescriptorPollService : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly RuntimeMicrostateAccounting accounting;
    private readonly RuntimeWorkerCreationPolicy? creation;
    private readonly Action<int>? beforePoll;
    private readonly Thread thread;
    private readonly Dictionary<ulong, LinuxDescriptorPollWait> waits = new();
    private readonly HashSet<LinuxDescriptorPollOwner> owners = new();
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int wakeRead, wakeWrite;
    private ulong nextOwner, nextRequest;
    private bool startAttempted, stopping, wakeClosed, physicalLifetimeEnded, actualThreadJoined;
    private Exception? failure;
    private readonly TaskCompletionSource shutdown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool shutdownStarted;
    private int actualThreadId;

    internal LinuxDescriptorPollService(
        RuntimeMicrostateAccounting accounting,
        RuntimeWorkerCreationPolicy? creation = null,
        Action<int>? beforePoll = null
    )
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        ArgumentNullException.ThrowIfNull(accounting);
        this.accounting = accounting;
        this.creation = creation;
        this.beforePoll = beforePoll;
        thread = new Thread(Run)
        {
            Name = LinuxDescriptorPollNames.ThreadName,
            IsBackground = true
        };
        int[] descriptors = [LinuxDescriptorPollNames.InvalidDescriptor, LinuxDescriptorPollNames.InvalidDescriptor];
        if (LinuxPortNative.pipe2(descriptors, LinuxPortParameters.NonBlocking | LinuxPortParameters.CloseOnExec) != 0)
            throw new NativePortIOException(Marshal.GetLastPInvokeError());
        wakeRead = descriptors[LinuxDescriptorPollNames.PipeReadIndex];
        wakeWrite = descriptors[LinuxDescriptorPollNames.PipeWriteIndex];
    }

    internal Task Ready => ready.Task;

    internal int ActualThreadId
    {
        get
        {
            lock (gate) return actualThreadId;
        }
    }

    internal bool PhysicalLifetimeEnded
    {
        get
        {
            lock (gate) return physicalLifetimeEnded;
        }
    }

    internal bool ActualThreadJoined
    {
        get
        {
            lock (gate) return actualThreadJoined;
        }
    }

    internal void Start()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(stopping || wakeClosed, this);
            if (startAttempted) throw new InvalidOperationException(LinuxDescriptorPollNames.DuplicateStart);
            startAttempted = true;
        }
        try
        {
            creation?.BeforeStart(RuntimeWorkerCreationNames.Poll, LinuxDescriptorPollNames.PollIdentity);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(stopping || wakeClosed, this);
                bool suppress = !ExecutionContext.IsFlowSuppressed();
                AsyncFlowControl flow = default;
                try
                {
                    if (suppress) flow = ExecutionContext.SuppressFlow();
                    thread.Start();
                }
                finally
                {
                    if (suppress) flow.Undo();
                }
            }
        }
        catch (Exception error)
        {
            lock (gate)
            {
                failure ??= error;
                stopping = true;
                WakeUnderGate();
            }
            throw;
        }
    }

    internal LinuxDescriptorPollOwner RegisterBorrowed(int descriptor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(descriptor);
        lock (gate)
        {
            RequireReadyUnderGate();
            ulong generation = checked(nextOwner + 1);
            var owner = new LinuxDescriptorPollOwner(this, generation, descriptor);
            owners.Add(owner);
            nextOwner = generation;

            return owner;
        }
    }

    internal Task<short> WaitReadyAsync(
        LinuxDescriptorPollOwner owner,
        short events,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(owner);
        cancellation.ThrowIfCancellationRequested();
        if (events == 0 || (events & ~LinuxDescriptorPollNames.AllowedEvents) != 0)
            throw new ArgumentOutOfRangeException(nameof(events));
        var wait = new LinuxDescriptorPollWait(owner, events, cancellation);
        // Registration allocation is before admission. An already-canceled
        // callback may only mark this not-yet-admitted request, never complete it.
        wait.CancellationRegistration = cancellation.UnsafeRegister(static state =>
        {
            var actual = (LinuxDescriptorPollWait)state!;
            actual.Owner.Service.Cancel(actual);
        }, wait);
        try
        {
            lock (gate)
            {
                RequireReadyUnderGate();
                if (!ReferenceEquals(owner.Service, this))
                    throw new InvalidOperationException(LinuxDescriptorPollNames.ForeignOwner);
                if (owner.Closing) throw owner.Closed;
                RequireOwnerUnderGate(owner);
                wait.Generation = checked(nextRequest + 1);
                waits.Add(wait.Generation, wait);
                nextRequest = wait.Generation;
                owner.Pending++;
                wait.Admitted = true;
                // After admission only the actual poll thread settles the call.
                WakeUnderGate();
            }
        }
        catch
        {
            wait.CancellationRegistration.Dispose();
            throw;
        }

        return wait.Completion.Task;
    }

    internal Task Retire(LinuxDescriptorPollOwner owner)
    {
        bool complete;
        lock (gate)
        {
            if (!ReferenceEquals(owner.Service, this))
                throw new InvalidOperationException(LinuxDescriptorPollNames.ForeignOwner);
            if (owner.Closing) return owner.Retired.Task;
            RequireOwnerUnderGate(owner);
            owner.Closing = true;
            complete = owner.Pending == 0;
            if (complete) owners.Remove(owner);
            else WakeUnderGate();
        }
        if (complete) owner.Retired.TrySetResult();

        return owner.Retired.Task;
    }

    private void RequireReadyUnderGate()
    {
        if (failure is not null) throw new IOException(LinuxDescriptorPollNames.Closed, failure);
        ObjectDisposedException.ThrowIf(stopping || wakeClosed, this);
        if (!startAttempted || !ready.Task.IsCompletedSuccessfully)
            throw new InvalidOperationException(LinuxDescriptorPollNames.NotReady);
    }

    private void RequireOwnerUnderGate(LinuxDescriptorPollOwner owner)
    {
        if (!ReferenceEquals(owner.Service, this) || !owners.Contains(owner))
            throw new InvalidOperationException(LinuxDescriptorPollNames.ForeignOwner);
    }

    private void Cancel(LinuxDescriptorPollWait wait)
    {
        lock (gate)
        {
            wait.CancellationRequested = true;
            if (wait.Admitted && waits.TryGetValue(wait.Generation, out var current)
                && ReferenceEquals(current, wait)) WakeUnderGate();
        }
    }

    private unsafe void WakeUnderGate()
    {
        if (wakeClosed) return;
        byte value = LinuxDescriptorPollNames.WakeSignalValue;
        while (true)
        {
            nint count = LinuxPortNative.write(wakeWrite, (nint)(&value), LinuxDescriptorPollNames.WakeSignalBytes);
            if (count == LinuxDescriptorPollNames.WakeSignalBytes) return;
            int error = Marshal.GetLastPInvokeError();
            if (count < 0 && error == LinuxPortParameters.Interrupted) continue;
            if (count < 0 && error == LinuxPortParameters.WouldBlock) return; // Existing unread wake.
            failure ??= new NativePortIOException(count == 0 ? LinuxPortParameters.BrokenPipe : error);
            stopping = true;

            // A failed wake is not proof that an existing kernel poll returned.
            // Keep physical join pending if the OS cannot wake this owned pipe.
            return;
        }
    }

    private unsafe void DrainWake()
    {
        // One bounded read per physical poll iteration. Remaining bytes keep
        // the FD level-ready, while every iteration reconciles terminal waits.
        byte* buffer = stackalloc byte[LinuxDescriptorPollNames.WakeBufferBytes];
        nint count = LinuxPortNative.read(wakeRead, (nint)buffer, LinuxDescriptorPollNames.WakeBufferBytes);
        if (count > 0) return;
        int error = Marshal.GetLastPInvokeError();
        if (count < 0 && (error == LinuxPortParameters.Interrupted || error == LinuxPortParameters.WouldBlock)) return;
        throw new NativePortIOException(count == 0 ? LinuxPortParameters.BrokenPipe : error);
    }

    private void Run()
    {
        try
        {
            creation?.BeforeAccounting(RuntimeWorkerCreationNames.Poll, LinuxDescriptorPollNames.PollIdentity);
            RuntimeMicrostateThreadIdentity physical = RuntimeMicrostateThreadIdentity.Current;
            using var attachment = RuntimeMicrostateExecution.AttachConfiguredWorker(
                accounting,
                LinuxDescriptorPollNames.PollType,
                RuntimeMicrostate.CheckIo,
                physical,
                LinuxDescriptorPollNames.PollIdentity
            );
            lock (gate) actualThreadId = physical.OperatingSystemId;
            creation?.EnteredBeforeReady(RuntimeWorkerCreationNames.Poll, LinuxDescriptorPollNames.PollIdentity);
            ready.TrySetResult();
            RunPollLoop();
        }
        catch (Exception error)
        {
            lock (gate)
            {
                failure ??= error;
                stopping = true;
            }
            ready.TrySetException(error);
            _ = ready.Task.Exception;
        }
        finally
        {
            // No native poll invocation remains. Drain retained rows one at a
            // time without allocating an exception-path snapshot. A row stays
            // discoverable in waits until its settlement attempt completes.
            lock (gate)
            {
                stopping = true;
                foreach (var owner in owners) owner.Closing = true;
            }
            while (true)
            {
                LinuxDescriptorPollWait? remaining = null;
                lock (gate)
                {
                    using var iterator = waits.GetEnumerator();
                    if (iterator.MoveNext())
                    {
                        remaining = iterator.Current.Value;
                        if (!remaining.SelectionComplete) SelectTerminalUnderGate(remaining, failure);
                    }
                }
                if (remaining is null) break;
                CompleteOneOutsideGate(remaining);
            }
            while (true)
            {
                LinuxDescriptorPollOwner? idle = null;
                lock (gate)
                {
                    using var iterator = owners.GetEnumerator();
                    if (iterator.MoveNext()) idle = iterator.Current;
                }
                if (idle is null) break;
                idle.Retired.TrySetResult();
                lock (gate) owners.Remove(idle);
            }
        }
    }

    private void RunPollLoop()
    {
        while (true)
        {
            // The previous kernel invocation has returned (or none has begun).
            // New cancellation/owner-close commands may concern rows absent
            // from its old snapshot. Retire ALL such rows before blocking again.
            LinuxDescriptorPollWait[] terminal;
            lock (gate)
            {
                if (stopping) return;
                terminal = waits.Values.Where(wait => wait.Owner.Closing
                    || wait.CancellationRequested || wait.Cancellation.IsCancellationRequested).ToArray();
                foreach (var wait in terminal)
                {
                    SelectTerminalUnderGate(wait, failure);
                }
            }
            CompleteOutsideGate(terminal);
            LinuxDescriptorPollWait[] snapshot;
            DescriptorPollState[] descriptors;
            bool stop;
            lock (gate)
            {
                stop = stopping;
                snapshot = waits.Values.ToArray();
                descriptors = new DescriptorPollState[snapshot.Length + LinuxDescriptorPollNames.FirstRequestIndex];
                descriptors[LinuxDescriptorPollNames.WakePollIndex] = new()
                {
                    Descriptor = wakeRead,
                    Events = DescriptorPortParameters.PollInput
                };
                for (int index = 0; index < snapshot.Length; index++)
                    descriptors[index + LinuxDescriptorPollNames.FirstRequestIndex] = new()
                    {
                        Descriptor = snapshot[index].Owner.Descriptor,
                        Events = snapshot[index].Events
                    };
            }
            if (stop) return;
            beforePoll?.Invoke(snapshot.Length);
            int result;
            int error;
            using (RuntimeMicrostateExecution.EnterState(accounting, RuntimeMicrostate.Sleep))
            {
                result = LinuxDescriptorPollNative.poll(
                    descriptors,
                    (nuint)descriptors.Length,
                    LinuxDescriptorPollNames.InfiniteTimeout
                );
                error = result < 0 ? Marshal.GetLastPInvokeError() : 0;
            }
            if (result < 0 && error != LinuxPortParameters.Interrupted)
                throw new NativePortIOException(error);
            short wakeEvents = descriptors[LinuxDescriptorPollNames.WakePollIndex].Returned;
            if ((wakeEvents & (LinuxDescriptorPollNames.Error | LinuxDescriptorPollNames.Hangup
                | DescriptorPortParameters.PollInvalid)) != 0)
                throw new NativePortIOException(LinuxPortParameters.BadDescriptor);
            if ((wakeEvents & DescriptorPortParameters.PollInput) != 0) DrainWake();
            var completed = new List<LinuxDescriptorPollWait>(snapshot.Length);
            lock (gate)
            {
                for (int index = 0; index < snapshot.Length; index++)
                {
                    var wait = snapshot[index];
                    if (!waits.TryGetValue(wait.Generation, out var current) || !ReferenceEquals(current, wait)) continue;
                    short returned = descriptors[index + LinuxDescriptorPollNames.FirstRequestIndex].Returned;
                    if (stopping || wait.Owner.Closing || wait.CancellationRequested || wait.Cancellation.IsCancellationRequested)
                        SelectTerminalUnderGate(wait, failure);
                    else if (result < 0 || returned == 0) continue;
                    else if ((returned & DescriptorPortParameters.PollInvalid) != 0)
                        wait.Error = new NativePortIOException(LinuxPortParameters.BadDescriptor);
                    else wait.ReadyEvents = returned; // HUP/ERR are advisory readiness; actual transfer decides EOF/EPIPE.
                    wait.SelectionComplete = true;
                    completed.Add(wait);
                }
            }
            // Every completed row's exact native snapshot has physically returned.
            CompleteOutsideGate(completed);
        }
    }

    private static void SelectTerminalUnderGate(LinuxDescriptorPollWait wait, Exception? error)
    {
        if (error is not null) wait.Error = error;
        else if (wait.CancellationRequested || wait.Cancellation.IsCancellationRequested) wait.CancelResult = true;
        else wait.Error = wait.Owner.Closed;
        wait.SelectionComplete = true;
    }

    private void CompleteOutsideGate(IEnumerable<LinuxDescriptorPollWait> completed)
    {
        foreach (var wait in completed) CompleteOneOutsideGate(wait);
    }

    private void CompleteOneOutsideGate(LinuxDescriptorPollWait wait)
    {
        Exception? unregister = null;
        try
        {
            wait.CancellationRegistration.Dispose();
        }
        catch (Exception error)
        {
            unregister = error;
        }
        if (unregister is not null)
        {
            if (wait.Error is not null) unregister = new AggregateException(wait.Error, unregister);
            wait.Completion.TrySetException(unregister);
            lock (gate) failure ??= unregister;
        }
        else if (wait.CancelResult) wait.Completion.TrySetCanceled(wait.Cancellation);
        else if (wait.Error is not null) wait.Completion.TrySetException(wait.Error);
        else wait.Completion.TrySetResult(wait.ReadyEvents);
        bool retired;
        lock (gate)
        {
            if (!wait.Admitted || !waits.TryGetValue(wait.Generation, out var current)
                || !ReferenceEquals(current, wait)) return;
            waits.Remove(wait.Generation);
            wait.Admitted = false;
            wait.Owner.Pending--;
            retired = wait.Owner.Closing && wait.Owner.Pending == 0;
        }
        if (retired)
        {
            wait.Owner.Retired.TrySetResult();
            lock (gate) owners.Remove(wait.Owner);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (ReferenceEquals(Thread.CurrentThread, thread))
            throw new InvalidOperationException(LinuxDescriptorPollNames.SelfJoin);
        lock (gate)
        {
            if (shutdownStarted) return new(shutdown.Task);
            shutdownStarted = true;
            stopping = true;
            try
            {
                WakeUnderGate();
            }
            catch (Exception error)
            {
                // Retain the original failure, but still attempt actual join
                // and owned-descriptor cleanup outside the service gate.
                failure ??= error;
            }
        }
        // This internal owner joins synchronously. Its VM lifetime caller is
        // already resumed asynchronously after closing and the port drain.
        try
        {
            JoinAndClose();
            shutdown.TrySetResult();
        }
        catch (Exception error)
        {
            shutdown.TrySetException(error);
        }

        return new(shutdown.Task);
    }

    private void JoinAndClose()
    {
        // Join the actual thread, not a completion-shaped substitute task.
        bool started = (thread.ThreadState & ThreadState.Unstarted) == 0;
        if (started) thread.Join();
        Exception? primary;
        int readError = 0, writeError = 0;
        bool readFailed, writeFailed;
        lock (gate)
        {
            physicalLifetimeEnded = true;
            actualThreadJoined = started;
            wakeClosed = true; // Stops all future cancellation callback writes before close/reuse.
            primary = failure;
            int read = wakeRead, write = wakeWrite;
            wakeRead = wakeWrite = LinuxDescriptorPollNames.InvalidDescriptor;
            // Attempt both native closes before allocating exception/report state.
            // Capture each errno immediately: the next close may replace it.
            readFailed = LinuxDescriptorPollNative.close(read) != 0;
            if (readFailed) readError = Marshal.GetLastPInvokeError();
            writeFailed = LinuxDescriptorPollNative.close(write) != 0;
            if (writeFailed) writeError = Marshal.GetLastPInvokeError();
        }
        ready.TrySetException(new ObjectDisposedException(LinuxDescriptorPollNames.Closed));
        _ = ready.Task.Exception;
        if (primary is not null || readFailed || writeFailed)
        {
            var errors = new List<Exception>();
            if (primary is not null) errors.Add(primary);
            if (readFailed) errors.Add(new NativePortIOException(readError));
            if (writeFailed) errors.Add(new NativePortIOException(writeError));

            throw new AggregateException(errors);
        }
    }
}
