namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal Task ObserveNormalWorkers(ProcessContext requester, Action<int> body)
        => ObserveNormalWorkers(requester, body, null);

    private Task ObserveNormalWorkers(ProcessContext requester, Action<int> body, Action? prepare)
    {
        ArgumentNullException.ThrowIfNull(body);
        var batch = new RuntimeSchedulerAuxiliaryBatch420(this, Capacity, body);
        int ownLane;
        lock (gate)
        {
            requester.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(retiring, this);
            var scheduler = requester.ActorTaskScheduler;
            if (!requester.Runtime.OwnsActorDispatcher428(this)
                || !RuntimeActorExecutionTurn.IsOwnedBy(requester)
                || DirtyNativeThread419.Owner is not null
                || scheduler is null || !ReferenceEquals(scheduler.Owner, requester)
                || !scheduler.Running || !ReferenceEquals(scheduler.Lane.Running, scheduler))
                throw new InvalidOperationException(RuntimeNormalObservationNames.Requester);
            if (normalBarrier696 is { Granted696: true })
                throw new InvalidOperationException(RuntimeNormalObservationNames.Barrier);
            ownLane = scheduler.Lane.Identity;
            try
            {
                prepare?.Invoke();
                batch.State = RuntimeSchedulerAuxiliaryState420.Published;
                for (int index = 0; index < lanes.Length; index++)
                    if (lanes[index].Identity != ownLane)
                        lanes[index].Auxiliary420.AddLast(batch.Work[index].Node);
                StartWorkers();
            }
            catch (Exception error)
            {
                batch.State = RuntimeSchedulerAuxiliaryState420.Aborted;
                foreach (RuntimeSchedulerAuxiliary420 work in batch.Work)
                {
                    work.Node.List?.Remove(work.Node);
                    work.Fail(error);
                }
                throw;
            }
        }
        // The authenticated requester's own normal reply runs on its actual
        // current turn. Other lanes acknowledge only by running their real jobs.
        // Never acquire normalBarrier696 around this batch: it excludes Aux jobs.
        batch.Work[ownLane - ProcessSchedulerNames.First].Execute(ownLane);

        return batch.Completion;
    }
}
