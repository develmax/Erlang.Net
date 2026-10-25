namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal int AssignedLane420(ProcessContext context)
    {
        if (RuntimeActorExecutionTurn.IsOwnedBy(context)) return RuntimeActorExecutionTurn.LaneIdentity;
        lock (gate)
        {
            var scheduler = context.ActorTaskScheduler;
            if (scheduler is null || !ReferenceEquals(scheduler.Owner, context)) throw new InvalidOperationException(RuntimeAuxiliaryNames420.MissingLane);

            return scheduler.Lane.Identity;
        }
    }

    internal RuntimeSchedulerAuxiliaryBatch420 PrepareAuxiliary420(Action<int> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return new(this, Capacity, body);
    }

    internal void PublishAuxiliary420(RuntimeSchedulerAuxiliaryBatch420 batch)
    {
        lock (gate)
        {
            if (!ReferenceEquals(batch.Owner, this) || batch.State != RuntimeSchedulerAuxiliaryState420.Prepared) throw new InvalidOperationException(RuntimeAuxiliaryNames420.ForeignBatch);
            try
            {
                ObjectDisposedException.ThrowIf(retiring, this);
                batch.State = RuntimeSchedulerAuxiliaryState420.Published;
                // Nodes/tasks/aggregate are all allocated before publication. No guest callback here.
                for (int i = 0; i < lanes.Length; i++) lanes[i].Auxiliary420.AddLast(batch.Work[i].Node);
                StartWorkers();
            }
            catch (Exception error)
            {
                batch.State = RuntimeSchedulerAuxiliaryState420.Aborted;
                foreach (var work in batch.Work)
                {
                    work.Node.List?.Remove(work.Node);
                    work.Fail(error);
                }
                throw;
            }
        }
    }

    internal void AbortAuxiliary420(RuntimeSchedulerAuxiliaryBatch420 batch, Exception error)
    {
        lock (gate)
        {
            if (!ReferenceEquals(batch.Owner, this) || batch.State == RuntimeSchedulerAuxiliaryState420.Published) throw new InvalidOperationException(RuntimeAuxiliaryNames420.ForeignBatch);
            if (batch.State == RuntimeSchedulerAuxiliaryState420.Aborted) return;
            batch.State = RuntimeSchedulerAuxiliaryState420.Aborted;
            foreach (var work in batch.Work) work.Fail(error);
        }
    }
}
