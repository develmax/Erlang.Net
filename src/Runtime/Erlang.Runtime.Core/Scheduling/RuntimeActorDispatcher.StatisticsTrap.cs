namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private Task ObserveStatisticsTrapNormalWorkers(
        ProcessContext requester,
        Action<int> body,
        Action? prepare,
        StatisticsTrapRequest trap
    )
    {
        var batch = new RuntimeSchedulerAuxiliaryBatch420(this, Capacity, body);
        trap.Observe(batch);
        int ownLane;
        lock (gate)
        {
            try
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
                prepare?.Invoke();
                batch.State = RuntimeSchedulerAuxiliaryState420.Published;
                for (int index = 0; index < lanes.Length; index++)
                    if (lanes[index].Identity != ownLane)
                        lanes[index].Auxiliary420.AddLast(batch.Work[index].Node);
                StartWorkers();
            }
            catch (Exception error)
            {
                // Abort before releasing D: no queued callback can race this cleanup.
                batch.State = RuntimeSchedulerAuxiliaryState420.Aborted;
                foreach (RuntimeSchedulerAuxiliary420 work in batch.Work)
                {
                    work.Node.List?.Remove(work.Node);
                    work.Fail(error);
                }
                throw;
            }
        }
        batch.Work[ownLane - ProcessSchedulerNames.First].Execute(ownLane);

        return batch.Completion;
    }

    internal (bool Previous, Task Completion) ChangeWallTimeWithStatisticsTrap(ProcessContext requester,
        bool enabled, StatisticsTrapRequest trap)
    {
        bool previous = false;
        Task completion = ObserveStatisticsTrapNormalWorkers(
            requester,
            identity =>
        {
            lock (gate)
                SetLaneWallTime(lanes[identity - ProcessSchedulerNames.First], enabled);
            trap.Reply();
        },
            () =>
        {
            previous = wallTimeEnabled;
            if (!previous && enabled)
                wallTimeGeneration++;
            wallTimeEnabled = enabled;
        },
            trap
        );

        return (previous, completion);
    }

    internal Task GatherWallTimeWithStatisticsTrap(
        ProcessContext requester,
        RuntimeWallTimeObservation observation,
        DirtyNativeOwner419 dirty,
        bool includeIo,
        StatisticsTrapRequest trap
    )
    {
        int own = AssignedLane420(requester);

        return ObserveStatisticsTrapNormalWorkers(
            requester,
            identity =>
        {
            RuntimeManagedSchedulerWallTimeSample? sample;
            lock (gate)
            {
                RuntimeSchedulerLane lane = lanes[identity - ProcessSchedulerNames.First];
                sample = lane.WallTimeEnabled ? SnapshotLaneWallTime(lane) : null;
                observation.Normal[identity - ProcessSchedulerNames.First] = sample;
            }
            if (identity == own)
                observation.Dirty = dirty.WallTimeSnapshot(includeIo);
            Term payload;
            if (sample is null)
                payload = Term.A(CommonAtomNames.Undefined);
            else
            {
                Term ownTuple = Term.Tuple(Term.I(identity), Term.I(sample.ActiveTicks), Term.I(sample.TotalTicks));
                if (identity != own)
                    payload = ownTuple;
                else
                {
                    var replies = new List<Term> { ownTuple };
                    RuntimeDirtyWallTimeSample[] dirtyRows = observation.Dirty!;
                    for (int index = 0; index < dirtyRows.Length; index++)
                        replies.Add(Term.Tuple(Term.I(Capacity + index + 1), Term.I(dirtyRows[index].Active), Term.I(dirtyRows[index].Total)));
                    payload = Cons.From(replies);
                }
            }
            // Exactly N normal replies; dirty rows travel in the requester's ONE list.
            trap.Reply(payload);
        },
            null,
            trap
        );
    }
}
