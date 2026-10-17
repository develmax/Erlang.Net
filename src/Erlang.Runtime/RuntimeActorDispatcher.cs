namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private readonly object gate = new();
    private readonly RuntimeSchedulerLane[] lanes;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly LinkedList<RuntimeSchedulerOnlineRequest> onlineRequests = new();
    private RuntimeSchedulerOnlineRequest? onlineChange;
    private RuntimeSchedulerOnlineRequest? onlineTurn;
    private int active;
    private int guests;
    private RuntimeProcessTaskScheduler? initializer;
    private Task initializerStop = Task.CompletedTask;
    private bool initializerStopRequested;
    private bool retiring;

    public int Capacity => lanes.Length;
    public Task Completion => completion.Task;
    internal int OnlineCount { get; private set; }

    public RuntimeActorDispatcher(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        lanes = Enumerable.Range(ProcessSchedulerNames.First, capacity).Select(identity => new RuntimeSchedulerLane(identity)).ToArray();
        OnlineCount = capacity;
    }

    public Task Dispatch(ProcessContext owner, Func<Task> body, RuntimeActorRole role = RuntimeActorRole.Guest)
    {
        ArgumentNullException.ThrowIfNull(body);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            var scheduler = new RuntimeProcessTaskScheduler(this, owner, role);
            scheduler.Lane = lanes[InitialLane(owner) - ProcessSchedulerNames.First];
            owner.ActorTaskScheduler = scheduler;
            active++;
            if (role == RuntimeActorRole.Initialization)
                initializer = scheduler;
            else
                guests++;
            Task execution;
            try
            {
                var factory = new TaskFactory(
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskContinuationOptions.None,
                    scheduler
                );
                execution = factory.StartNew(NifImageInitializationFlow.IsolateActor(NifRetirementFlow.IsolateActor(body))).Unwrap();
                scheduler.CancellationRegistration = owner.Cancellation.UnsafeRegister(static value =>
                {
                    var target = (RuntimeProcessTaskScheduler)value!;
                    target.NotifyCancellation();
                }, scheduler);
            }
            catch
            {
                active--;
                if (role == RuntimeActorRole.Guest)
                    guests--;
                else
                    initializer = null;
                throw;
            }
            _ = execution.ContinueWith(
                _ => Released(scheduler),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );

            return execution;
        }
    }

    private int InitialLane(ProcessContext owner)
    {
        if (owner.SchedulerBinding != ProcessSchedulerNames.Unbound)
            return owner.SchedulerBinding;
        int actual = RuntimeActorExecutionTurn.IsRuntime(owner.Runtime) ? RuntimeActorExecutionTurn.LaneIdentity : ProcessSchedulerNames.Unbound;

        return actual >= ProcessSchedulerNames.First && actual <= OnlineCount ? actual : ProcessSchedulerNames.First;
    }

    internal void Enqueue(RuntimeProcessTaskScheduler scheduler, Task task)
    {
        lock (gate)
        {
            long activeEpoch = RuntimeActorExecutionTurn.Epoch(scheduler.Owner);
            ProcessExecutionAdmission admission = task.AsyncState is RuntimeActorContinuation continuation
                ? continuation.Admission : ProcessExecutionAdmission.Continuation;
            bool freshWakeup = activeEpoch == 0 && !scheduler.Running && scheduler.Pending.Count == 0;
            long epoch = admission == ProcessExecutionAdmission.Reduction || scheduler.NextEpoch == 0 || freshWakeup
                ? ++scheduler.NextEpoch : activeEpoch == 0 ? scheduler.NextEpoch : activeEpoch;
            scheduler.Pending.Enqueue(new RuntimeScheduledTask(task, epoch));
            if (!scheduler.Running && scheduler.ReadyNode is null)
                Ready(scheduler);
            StartWorkers();
        }
    }

    internal Task[] Snapshot(RuntimeProcessTaskScheduler scheduler)
    {
        lock (gate)
            return scheduler.Pending.Select(work => work.Task).ToArray();
    }

    internal (int Online, int Active) State()
    {
        lock (gate)
            return (OnlineCount, lanes.Count(lane => lane.Online || lane.Running is not null));
    }

    internal void PriorityChanged(ProcessContext owner)
    {
        lock (gate)
        {
            if (owner.ActorTaskScheduler is not { ReadyNode: { } node } scheduler)
                return;
            node.List!.Remove(node);
            scheduler.ReadyNode = null;
            Ready(scheduler, true);
            StartWorkers();
        }
    }

    internal int ChangeBinding(ProcessContext owner, int binding)
    {
        lock (gate)
        {
            var scheduler = owner.ActorTaskScheduler ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
            int previous = owner.SchedulerBinding;
            owner.SchedulerBinding = binding;
            if (scheduler.ReadyNode is { } node)
            {
                node.List!.Remove(node);
                scheduler.ReadyNode = null;
            }
            if (!scheduler.Running)
            {
                AssignLane(scheduler);
                if (scheduler.Pending.Count != 0)
                    Ready(scheduler);
            }
            StartWorkers();

            return previous;
        }
    }

    internal async Task<int> ChangeOnline(ProcessContext owner, int count)
    {
        if (owner.ActorTaskScheduler is null || !RuntimeActorExecutionTurn.IsOwnedBy(owner))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var request = new RuntimeSchedulerOnlineRequest(owner, count);
        using var cancellation = owner.Cancellation.UnsafeRegister(_ => CancelOnline(request), null);
        lock (gate)
        {
            owner.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(retiring, this);
            request.Node = onlineRequests.AddLast(request);
            GrantOnlineTurn();
        }
        try
        {
            // Native waiters resume and retry in their own process. Merely making
            // a permit ready must not apply a change for an offline bound owner.
            await owner.Mailbox.WaitForControlCompletion(request.Admitted.Task, owner.Cancellation);
            owner.Runtime.AdmitSignals(owner);
            owner.Cancellation.ThrowIfCancellationRequested();
            lock (gate)
            {
                owner.Cancellation.ThrowIfCancellationRequested();
                if (!ReferenceEquals(onlineTurn, request))
                    throw new InvalidOperationException(ProcessSchedulerNames.OnlineTurnMismatch);
                onlineRequests.Remove(request.Node!);
                request.Node = null;
                onlineTurn = null;
                onlineChange = request;
                request.Previous = OnlineCount;
                ApplyOnline(request.Requested);
                CompleteTransition();
                StartWorkers();
            }
            await owner.Mailbox.WaitForControlCompletion(request.Completed.Task, owner.Cancellation);
            owner.Runtime.AdmitSignals(owner);
            owner.Cancellation.ThrowIfCancellationRequested();

            return request.Previous;
        }
        finally
        {
            cancellation.Unregister();
        }
    }

    private void ApplyOnline(int count)
    {
        OnlineCount = count;
        foreach (var lane in lanes)
        {
            lane.Online = lane.Identity <= count;
            if (lane.Online)
                continue;
            foreach (var queue in new[] { lane.Maximum, lane.High, lane.Ordinary, lane.Cleanup })
            {
                var node = queue.First;
                while (node is not null)
                {
                    var next = node.Next;
                    if (node.Value.Owner.SchedulerBinding == ProcessSchedulerNames.Unbound)
                    {
                        var scheduler = node.Value;
                        queue.Remove(node);
                        scheduler.ReadyNode = null;
                        AssignLane(scheduler);
                        Ready(scheduler);
                    }
                    node = next;
                }
            }
        }
    }

    private void GrantOnlineTurn()
    {
        if (onlineChange is not null || onlineTurn is not null)
            return;
        while (onlineRequests.First is { } first)
        {
            var request = first.Value;
            if (request.Owner.Cancellation.IsCancellationRequested)
            {
                onlineRequests.RemoveFirst();
                request.Node = null;
                request.Admitted.TrySetCanceled(request.Owner.Cancellation);
                request.Completed.TrySetCanceled(request.Owner.Cancellation);
                continue;
            }
            onlineTurn = request;
            request.Admitted.TrySetResult();

            return;
        }
    }

    private void CancelOnline(RuntimeSchedulerOnlineRequest request)
    {
        lock (gate)
        {
            request.Admitted.TrySetCanceled(request.Owner.Cancellation);
            request.Completed.TrySetCanceled(request.Owner.Cancellation);
            if (request.Node is { } node)
            {
                onlineRequests.Remove(node);
                request.Node = null;
            }
            if (ReferenceEquals(onlineTurn, request))
                onlineTurn = null;
            // An applied topology change completes independently of its caller;
            // cancellation cannot grant a new change before old lanes quiesce.
            if (!ReferenceEquals(onlineChange, request))
                GrantOnlineTurn();
        }
    }

    private void AssignLane(RuntimeProcessTaskScheduler scheduler)
    {
        int binding = scheduler.Owner.SchedulerBinding;
        if (binding != ProcessSchedulerNames.Unbound)
            scheduler.Lane = lanes[binding - ProcessSchedulerNames.First];
        else if (!scheduler.Lane.Online)
            scheduler.Lane = lanes[ProcessSchedulerNames.Unbound];
    }

    private void Ready(RuntimeProcessTaskScheduler scheduler, bool reprioritized = false)
    {
        AssignLane(scheduler);
        bool sameTurn = !reprioritized && scheduler.LastExecutedEpoch != 0 &&
            scheduler.Pending.Peek().Epoch == scheduler.LastExecutedEpoch;
        scheduler.SkipCount = !sameTurn && scheduler.Owner.SchedulingPriorityLevel == ProcessSchedulingPriority.Low
            ? ProcessPriorityNames.LowRescheduleCount : 1;
        var queue = scheduler.Owner.Cancellation.IsCancellationRequested && (scheduler.Lane.Online || retiring)
            ? scheduler.Lane.Cleanup : scheduler.Lane.Queue(scheduler.Owner.SchedulingPriorityLevel);
        scheduler.ReadyNode = sameTurn ? queue.AddFirst(scheduler) : queue.AddLast(scheduler);
    }

    internal void OwnerCanceled(RuntimeProcessTaskScheduler scheduler)
    {
        lock (gate)
        {
            if (scheduler.ReadyNode is { } node && (scheduler.Lane.Online || retiring))
            {
                node.List!.Remove(node);
                scheduler.ReadyNode = scheduler.Lane.Cleanup.AddLast(scheduler);
            }
            StartWorkers();
        }
    }

    private RuntimeProcessTaskScheduler? Select(RuntimeSchedulerLane lane)
    {
        LinkedList<RuntimeProcessTaskScheduler> queue;
        if (lane.Cleanup.Count != 0 && (lane.Online || retiring))
            queue = lane.Cleanup;
        else if (!lane.Online)
            return null;
        else if (lane.Maximum.Count != 0)
            queue = lane.Maximum;
        else if (lane.High.Count != 0)
            queue = lane.High;
        else
            queue = lane.Ordinary;
        if (queue.First is null)
            return null;
        while (ReferenceEquals(queue, lane.Ordinary) && queue.Count > 1 &&
            queue.First!.Value.Owner.SchedulingPriorityLevel == ProcessSchedulingPriority.Low &&
            --queue.First.Value.SkipCount > 0)
        {
            var skipped = queue.First!;
            queue.RemoveFirst();
            queue.AddLast(skipped);
        }
        var best = queue.First!.Value;
        queue.RemoveFirst();
        best.ReadyNode = null;
        best.Running = true;
        lane.Running = best;

        return best;
    }

    private void StartWorkers()
    {
        foreach (var lane in lanes)
        {
            if (lane.Online && !lane.Pumping && lane.Empty)
                TrySteal(lane);
            if (lane.Pumping || lane.Empty || !lane.Online && (!retiring || lane.Cleanup.Count == 0))
                continue;
            lane.Pumping = true;
            if (ExecutionContext.IsFlowSuppressed())
                _ = Task.Run(() => Pump(lane));
            else
                using (ExecutionContext.SuppressFlow())
                    _ = Task.Run(() => Pump(lane));
        }
    }

    private void TrySteal(RuntimeSchedulerLane destination)
    {
        foreach (var source in lanes)
        {
            if (ReferenceEquals(source, destination) || !source.Online)
                continue;
            foreach (var queue in new[] { source.Cleanup, source.Maximum, source.High, source.Ordinary })
            {
                for (var node = queue.First; node is not null; node = node.Next)
                {
                    var scheduler = node.Value;
                    if (scheduler.Owner.SchedulerBinding != ProcessSchedulerNames.Unbound)
                        continue;
                    queue.Remove(node);
                    scheduler.Lane = destination;
                    var destinationQueue = ReferenceEquals(queue, source.Cleanup)
                        ? destination.Cleanup : destination.Queue(scheduler.Owner.SchedulingPriorityLevel);
                    scheduler.ReadyNode = destinationQueue.AddLast(scheduler);

                    return;
                }
            }
        }
    }

    private void Pump(RuntimeSchedulerLane lane)
    {
        while (true)
        {
            RuntimeProcessTaskScheduler? scheduler;
            RuntimeScheduledTask task;
            lock (gate)
            {
                scheduler = Select(lane);
                if (scheduler is null && lane.Online)
                {
                    TrySteal(lane);
                    scheduler = Select(lane);
                }
                if (scheduler is null)
                {
                    lane.Pumping = false;
                    CompleteTransition();
                    CompleteIfIdle();

                    return;
                }
                task = scheduler.Pending.Dequeue();
                scheduler.LastExecutedEpoch = task.Epoch;
            }
            try
            {
                scheduler.Execute(task, lane.Identity);
            }
            finally
            {
                lock (gate)
                {
                    scheduler.Running = false;
                    lane.Running = null;
                    AssignLane(scheduler);
                    if (scheduler.Pending.Count != 0)
                        Ready(scheduler);
                    CompleteTransition();
                    StartWorkers();
                }
            }
        }
    }

    private void CompleteTransition()
    {
        if (onlineChange is not { } completed || lanes.Any(lane => !lane.Online && lane.Running is not null))
            return;
        onlineChange = null;
        completed.Completed.TrySetResult();
        GrantOnlineTurn();
    }

    public Task CompleteWhenIdle()
    {
        lock (gate)
        {
            retiring = true;
            StopInitializerAfterGuests();
            foreach (var lane in lanes)
                foreach (var queue in new[] { lane.Maximum, lane.High, lane.Ordinary })
                {
                    var node = queue.First;
                    while (node is not null)
                    {
                        var next = node.Next;
                        if (node.Value.Owner.Cancellation.IsCancellationRequested)
                        {
                            queue.Remove(node);
                            node.Value.ReadyNode = lane.Cleanup.AddLast(node.Value);
                        }
                        node = next;
                    }
                }
            StartWorkers();
            CompleteIfIdle();

            return completion.Task;
        }
    }

    private void Released(RuntimeProcessTaskScheduler scheduler)
    {
        scheduler.CancellationRegistration.Unregister();
        lock (gate)
        {
            active--;
            if (scheduler.Role == RuntimeActorRole.Guest)
                guests--;
            StopInitializerAfterGuests();
            CompleteIfIdle();
        }
    }

    private void StopInitializerAfterGuests()
    {
        if (!retiring || guests != 0 || initializer is null || initializerStopRequested)
            return;
        initializerStopRequested = true;
        var owner = initializer.Owner;
        if (ExecutionContext.IsFlowSuppressed())
            initializerStop = Task.Run(() => owner.Stop.CancelAsync());
        else
            using (ExecutionContext.SuppressFlow())
                initializerStop = Task.Run(() => owner.Stop.CancelAsync());
        _ = initializerStop.ContinueWith(
            _ =>
        {
            lock (gate)
                CompleteIfIdle();
        },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    private void CompleteIfIdle()
    {
        if (!retiring || active != 0 || !initializerStop.IsCompleted || lanes.Any(lane => lane.Pumping || !lane.Empty))
            return;
        if (initializerStop.Exception is { } failure)
            completion.TrySetException(failure.InnerExceptions);
        else
            completion.TrySetResult();
    }
}
