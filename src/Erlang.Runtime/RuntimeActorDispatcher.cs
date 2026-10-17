namespace Erlang;

internal sealed class RuntimeActorDispatcher
{
    private readonly object gate = new();
    private readonly LinkedList<RuntimeProcessTaskScheduler> ready = new();
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int active;
    private int workers;
    private bool retiring;

    public int Capacity { get; }
    public Task Completion => completion.Task;

    public RuntimeActorDispatcher(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
    }

    public Task Dispatch(ProcessContext owner, Func<Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            var scheduler = new RuntimeProcessTaskScheduler(this, owner);
            owner.ActorTaskScheduler = scheduler;
            active++;
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
            }
            catch
            {
                active--;
                throw;
            }
            _ = execution.ContinueWith(
                _ => Released(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );

            return execution;
        }
    }

    internal void Enqueue(RuntimeProcessTaskScheduler scheduler, Task task)
    {
        lock (gate)
        {
            // Admitted actors retain continuation admission during retirement.
            // Canceled/terminated owners still execute tasks so their finally paths settle.
            long activeEpoch = RuntimeActorExecutionTurn.Epoch(scheduler.Owner);
            ProcessExecutionAdmission admission = task.AsyncState is RuntimeActorContinuation continuation
                ? continuation.Admission : ProcessExecutionAdmission.Continuation;
            bool freshWakeup = activeEpoch == 0 && !scheduler.Running && scheduler.Pending.Count == 0;
            long epoch;
            if (admission == ProcessExecutionAdmission.Reduction || scheduler.NextEpoch == 0 || freshWakeup)
                epoch = ++scheduler.NextEpoch;
            else
                epoch = activeEpoch == 0 ? scheduler.NextEpoch : activeEpoch;
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

    internal void PriorityChanged(ProcessContext owner)
    {
        lock (gate)
        {
            if (owner.ActorTaskScheduler is not { ReadyNode: { } node } scheduler)
                return;
            ready.Remove(node);
            scheduler.ReadyNode = null;
            Ready(scheduler, true);
        }
    }

    private void Ready(RuntimeProcessTaskScheduler scheduler, bool reprioritized = false)
    {
        bool sameTurn = !reprioritized && scheduler.LastExecutedEpoch != 0 &&
            scheduler.Pending.Peek().Epoch == scheduler.LastExecutedEpoch;
        scheduler.SkipCount = !sameTurn && scheduler.Owner.SchedulingPriorityLevel == ProcessSchedulingPriority.Low
            ? ProcessPriorityNames.LowRescheduleCount : 1;
        // A managed continuation within a logical reduction epoch continues that
        // turn; only an explicit reduction/new wake pays low's admission counter.
        scheduler.ReadyNode = sameTurn ? ready.AddFirst(scheduler) : ready.AddLast(scheduler);
    }

    private RuntimeProcessTaskScheduler? Select()
    {
        if (ready.Count == 0)
            return null;
        var best = ready.First!;
        foreach (var candidate in ready)
            if (candidate.Owner.SchedulingPriorityLevel > best.Value.Owner.SchedulingPriorityLevel)
                best = candidate.ReadyNode!;
        if (best.Value.Owner.SchedulingPriorityLevel < ProcessSchedulingPriority.High)
        {
            while (true)
            {
                best = ready.First!;
                if (best.Value.Owner.SchedulingPriorityLevel != ProcessSchedulingPriority.Low || --best.Value.SkipCount == 0)
                    break;
                ready.RemoveFirst();
                ready.AddLast(best);
            }
        }
        ready.Remove(best);
        best.Value.ReadyNode = null;
        best.Value.Running = true;

        return best.Value;
    }

    private void StartWorkers()
    {
        int additional = Math.Min(Capacity - workers, ready.Count);
        for (int index = 0; index < additional; index++)
        {
            workers++;
            // Task execution restores its captured context; dispatcher workers must
            // not inherit a spawning process's code/NIF/retirement AsyncLocals.
            if (ExecutionContext.IsFlowSuppressed())
                _ = Task.Run(Pump);
            else
                using (ExecutionContext.SuppressFlow())
                    _ = Task.Run(Pump);
        }
    }

    private void Pump()
    {
        while (true)
        {
            RuntimeProcessTaskScheduler? scheduler;
            RuntimeScheduledTask task;
            lock (gate)
            {
                scheduler = Select();
                if (scheduler is null)
                {
                    workers--;
                    CompleteIfIdle();

                    return;
                }
                task = scheduler.Pending.Dequeue();
                scheduler.LastExecutedEpoch = task.Epoch;
            }
            try
            {
                scheduler.Execute(task);
            }
            finally
            {
                lock (gate)
                {
                    scheduler.Running = false;
                    if (scheduler.Pending.Count != 0)
                        Ready(scheduler);
                }
            }
        }
    }

    public Task CompleteWhenIdle()
    {
        lock (gate)
        {
            retiring = true;
            CompleteIfIdle();

            return completion.Task;
        }
    }

    private void Released()
    {
        lock (gate)
        {
            active--;
            CompleteIfIdle();
        }
    }

    private void CompleteIfIdle()
    {
        if (retiring && active == 0 && workers == 0 && ready.Count == 0)
            completion.TrySetResult();
    }
}
