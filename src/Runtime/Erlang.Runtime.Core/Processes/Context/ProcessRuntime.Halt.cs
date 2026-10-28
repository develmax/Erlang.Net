using System.Diagnostics;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly TaskCompletionSource<RuntimeHaltResult> haltCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource haltQuiescence = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RuntimeHaltResult? haltResult;
    private int haltState;
    private bool haltFlushPending;

    public Task<RuntimeHaltResult> HaltCompletion => haltCompletion.Task;
    public Task HaltQuiescence => haltQuiescence.Task;
    public RuntimeLifecycleState Lifecycle => Volatile.Read(ref haltResult) is null ? disposed ? RuntimeLifecycleState.Disposed : RuntimeLifecycleState.Running : (RuntimeLifecycleState)Volatile.Read(ref haltState);

    public Task<RuntimeHaltResult> RequestImmediateHalt(int status) => RequestHalt(status, new RuntimeHaltOptions(false));

    public Task<RuntimeHaltResult> RequestHalt(int status, RuntimeHaltOptions? options = null)
        => RequestHalt(new(
            RuntimeHaltKind.Numeric,
            status,
            null,
            options ?? new()
        ));

    public Task<RuntimeHaltResult> RequestHalt(RuntimeHaltRequest request) => RequestHaltCore(request, false);

    private Task<RuntimeHaltResult> RequestHaltCore(RuntimeHaltRequest request, bool fatalAtomExhaustion)
    {
        if (!fatalAtomExhaustion)
            Modules.RequireSourceShutdownCaller();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Options);
        if (!Enum.IsDefined(request.Kind))
            throw new ArgumentOutOfRangeException(nameof(request));
        int status = request.Status;
        RuntimeHaltOptions options = request.Kind == RuntimeHaltKind.Numeric ? request.Options : new(false);
        ArgumentOutOfRangeException.ThrowIfNegative(status);
        ProcessContext[] stopped;
        Task[] executions;
        Task monitoring;
        Task portDrain;
        RuntimeHaltResult result;
        long selectedAt;
        CancellationTokenSource deadlineStop;
        Task deadline;
        lock (gate)
        {
            if (haltResult is not null)
            {
                if (haltFlushPending && !options.Flush && !haltCompletion.Task.IsCompleted)
                {
                    haltFlushPending = false;
                    result = new RuntimeHaltResult(status & RuntimeHaltNames.StatusMask) { Kind = request.Kind, Slogan = request.Slogan, LogicalStack = request.LogicalStack };
                    Volatile.Write(ref haltResult, result);
                    _ = Ports.ForceHalt();
                    haltCompletion.TrySetResult(result);
                }

                return haltCompletion.Task;
            }
            ObjectDisposedException.ThrowIf(disposed, this);
            result = new RuntimeHaltResult(status & RuntimeHaltNames.StatusMask) { Kind = request.Kind, Slogan = request.Slogan, LogicalStack = request.LogicalStack };
            haltFlushPending = options.Flush;
            selectedAt = Stopwatch.GetTimestamp();
            Volatile.Write(ref haltState, (int)RuntimeLifecycleState.Stopping);
            Volatile.Write(ref haltResult, result);
            StopLocalNode();
            localNodeEvents?.Close();
            disposed = true;
            StopMailboxRootAdmission593();
            _ = actorDispatcher.CompleteWhenIdle();
            distributionPending?.Close();
            OsEnvironment?.Dispose();
            CloseDistributionEntryIdentities();
            etsFoundation?.Close();
            _ = ClosePersistentTerms();
            _ = CloseSystemTasks428();
            _ = CloseDirtyNative419();
            _ = CloseDebugDispatch432();
            _ = CloseCounters421();
            _ = CloseHamtIndex429();
            _ = CloseRegex411();
            _ = CloseDriverFrontend422();
            _ = CloseAtomics413();
            CloseCollectors413();
            _ = CloseCodePurger432();
            ProcessTracing.CloseTraceSessions425();
            _ = CloseNativeResources524();
            ReferenceResources.Close();
            stopped = processes.Values.ToArray();
            executions = executingProcesses.Select(process => process.Unwound.Task).ToArray();
            ProcessTracing.StopForHalt();
            monitoring = SystemMonitoring.StopForHalt();
            SystemProfiling.ClearForHalt();
            SequenceTracing.StopForHalt(stopped.Concat(executingProcesses).Distinct());
            CloseProcessAlive425();
            internalGroupLeaderRequests425.Clear();
            processes.Clear();
            names.Clear();
            aliases.Clear();
            monitors.Clear();
            spawnRequests.Clear();
            foreach (ProcessContext process in stopped)
            {
                processSlots.Remove(process.Self);
                process.Terminated = true;
                process.Mailbox.StopAdmissionForExit606();
                process.ExitReason = Term.A(ProcessExitReasons.Shutdown);
                process.PendingCodeChecks.Clear();
                process.PendingInfoRequests.Clear();
                process.PendingDisplayRequests.Clear();
                process.PendingGarbageCollections.Clear();
                process.PendingSavedCallRequests.Clear();
                process.Links.Clear();
                process.PriorityLinks.Clear();
                process.PortLinks.Clear();
                process.PriorityPortLinks.Clear();
                if (etsFoundation?.HasPendingRetirements != true && PersistentTermRetirementCompletedSuccessfully && RegexRetirementCompleted411 && DirtyNativeRetirementCompleted419 && DebugDispatchRetirementCompleted432 && CountersRetirementCompleted421 && DriverFrontendRetirementCompleted422 && CodePurgerRetirementCompleted432 && AllocatorGatherRetirementCompleted434)
                    process.Done.TrySetResult(process.ExitReason);
            }
            deadlineStop = new CancellationTokenSource();
            deadline = options.Flush && options.FlushTimeoutMilliseconds is uint milliseconds ? WaitHaltDeadline(milliseconds, selectedAt, deadlineStop.Token) : Task.Delay(Timeout.Infinite, deadlineStop.Token);
            portDrain = Task.WhenAll(Ports.BeginHalt(options.Flush), DrainDriverFrontend422());
        }
        _ = Task.Run(NifImageInitializationFlow.IsolateActor(NifRetirementFlow.IsolateActor(() => FinishHalt(
            stopped,
            executions,
            monitoring,
            portDrain,
            options,
            deadline,
            deadlineStop
        ))));

        return haltCompletion.Task;
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal void Halt(ProcessContext context, int status)
    {
        if (!ReferenceEquals(context.Runtime, this))
            throw new ArgumentException(nameof(context));
        RequestImmediateHalt(status);
        throw new ProcessHaltTransfer(this);
    }

    internal bool IsHaltRequested => Volatile.Read(ref haltResult) is not null;

    internal void ThrowIfHalted()
    {
        if (IsHaltRequested)
            throw new ProcessHaltTransfer(this);
    }

    internal void SendPortNotification(Pid target, Term message)
    {
        lock (gate)
            if (!IsHaltRequested && processes.TryGetValue(target, out var receiver))
                receiver.Mailbox.Send(message);
    }

    private async Task FinishHalt(
        ProcessContext[] stopped,
        Task[] executions,
        Task monitoring,
        Task portDrain,
        RuntimeHaltOptions options,
        Task deadline,
        CancellationTokenSource deadlineStop
    )
    {
        var failures = new List<Exception>();
        var ownerCancellation = new List<Task>();
        try
        {
            Task timerCallbacks = Timers.StopForHaltAsync();
            Suspensions.StopForHalt();
            Task mailboxRetirements593;
            lock (gate)
                mailboxRetirements593 = Task.WhenAll(stopped.Select(process =>
                    process.Mailbox.DrainAfterQueueObservations606(EtsQueueRetirement419(process, Task.CompletedTask))));
            Volatile.Write(ref haltState, (int)RuntimeLifecycleState.Stopped);
            if (!options.Flush)
                haltCompletion.TrySetResult(haltResult!);
            foreach (ProcessContext process in stopped)
            {
                try
                {
                    ownerCancellation.Add(process.Stop.CancelAsync());
                }
                catch (Exception exception)
                {
                    ownerCancellation.Add(Task.FromException(exception));
                }
            }
            try
            {
                await CompleteHaltEtsRetirements(stopped).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            Task sourceShutdown = Modules.CloseSourceMutationsAsync();
            Task nativeShutdown;
            try
            {
                nativeShutdown = Modules.CloseNifLifetimeAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
                nativeShutdown = Task.CompletedTask;
            }
            if (options.Flush && !haltCompletion.Task.IsCompleted)
            {
                Task winner = await Task.WhenAny(deadline, portDrain, haltCompletion.Task).ConfigureAwait(false);
                lock (gate)
                {
                    if (!haltCompletion.Task.IsCompleted)
                    {
                        haltFlushPending = false;
                        if (winner == deadline)
                        {
                            Volatile.Write(ref haltResult, new RuntimeHaltResult(RuntimeHaltParameters.FlushTimeoutExitStatus));
                            _ = Ports.ForceHalt();
                        }
                        haltCompletion.TrySetResult(haltResult!);
                    }
                }
                deadlineStop.Cancel();
                try
                {
                    await deadline.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (deadlineStop.IsCancellationRequested)
                {
                }
            }
            else
                haltCompletion.TrySetResult(haltResult!);
            try
            {
                await Task.WhenAll(
                    initializer.Completion,
                    Task.WhenAll(executions),
                    timerCallbacks,
                    monitoring,
                    portDrain,
                    Task.WhenAll(ownerCancellation),
                    mailboxRetirements593
                ).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                await Ports.Drain().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                if (distributionPending is { } outputOwner)
                    await outputOwner.DrainOwnedOutputOperations().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                await Task.WhenAll(
                    DrainExitContinuations(),
                    DrainEtsRetirements(),
                    DrainPersistentTerms(),
                    DrainRegex411(),
                    DrainSystemTasks428(),
                    DrainDirtyNative419(),
                    DrainDebugDispatch432(),
                    DrainCounters421(),
                    DrainMailboxRoots593(),
                    DrainAtomics586(),
                    DrainHamtIndex429(),
                    DrainDriverFrontend422(),
                    DrainCodePurger432(),
                    DrainAllocatorGather434(),
                    DrainOwnedTermContexts506(),
                    DrainNativeResources524()
                ).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            bool nativeImagesSettled528 = false;
            try
            {
                await Task.WhenAll(sourceShutdown, nativeShutdown, DrainAutomaticNifShutdown()).ConfigureAwait(false);
                nativeImagesSettled528 = true;
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                FinalizeNativeAllocationArena528(nativeImagesSettled528);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (ownsStandardStreams)
                StandardStreams.Dispose();
            if (failures.Count != 0)
                throw new AggregateException(failures);
            haltQuiescence.TrySetResult();
        }
        catch (Exception exception)
        {
            haltCompletion.TrySetException(exception);
            haltQuiescence.TrySetException(exception);
        }
        finally
        {
            deadlineStop.Cancel();
            deadlineStop.Dispose();
        }
    }

    private static async Task WaitHaltDeadline(uint milliseconds, long started, CancellationToken cancellation)
    {
        while (true)
        {
            double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (elapsed >= milliseconds)
                return;
            int segment = (int)Math.Min(RuntimeHaltParameters.MaximumDelayMilliseconds, Math.Ceiling(milliseconds - elapsed));
            await Task.Delay(segment, cancellation).ConfigureAwait(false);
        }
    }
}
