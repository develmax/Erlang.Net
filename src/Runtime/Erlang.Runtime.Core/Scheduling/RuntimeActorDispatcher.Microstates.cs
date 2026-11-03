namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    // Prepared batch ownership is explicit. Only short metadata admission holds
    // R/D; unmanaged/local callbacks and instrumentation flip run afterwards.
    internal int? PublishMicrostateRequest(
        ProcessContext requester,
        RuntimeSchedulerAuxiliaryBatch420 batch,
        RuntimeMicrostateRequest request
    )
    {
        lock (gate)
        {
            if (!ReferenceEquals(batch.Owner, this) || batch.State != RuntimeSchedulerAuxiliaryState420.Prepared)
                throw new InvalidOperationException(RuntimeAuxiliaryNames420.ForeignBatch);
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
            int ownLane = scheduler.Lane.Identity;
            try
            {
                // BIF-requested exchange precedes the source local-TSD no-op.
                if (!request.Prepare()) return null;
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

            return ownLane;
        }
    }
}
