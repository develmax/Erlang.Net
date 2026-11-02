namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal (bool Previous, Task Completion) ChangeWallTime(ProcessContext requester, bool enabled)
    {
        bool previous = false;
        Task completion = ObserveNormalWorkers(
            requester,
            identity =>
        {
            lock (gate) SetLaneWallTime(lanes[identity - ProcessSchedulerNames.First], enabled);
        },
            () =>
        {
            // Pure typed state preparation, under dispatcher admission gate.
            previous = wallTimeEnabled;
            if (!previous && enabled) wallTimeGeneration++;
            wallTimeEnabled = enabled;
        }
        );

        return (previous, completion);
    }

    internal bool RequestingLaneWallTimeEnabled(ProcessContext requester)
    {
        lock (gate)
        {
            if (!RuntimeActorExecutionTurn.IsOwnedBy(requester)
                || requester.ActorTaskScheduler is not { } scheduler
                || !ReferenceEquals(scheduler.Owner, requester)
                || !ReferenceEquals(scheduler.Lane.Running, scheduler))
                throw new InvalidOperationException(RuntimeNormalObservationNames.Requester);

            return scheduler.Lane.WallTimeEnabled;
        }
    }

    internal Task GatherWallTime(
        ProcessContext requester,
        RuntimeWallTimeObservation observation,
        DirtyNativeOwner419 dirty,
        bool includeIo
    )
    {
        int own = AssignedLane420(requester);

        return ObserveNormalWorkers(
            requester,
            identity =>
        {
            lock (gate)
            {
                RuntimeSchedulerLane lane = lanes[identity - ProcessSchedulerNames.First];
                observation.Normal[identity - ProcessSchedulerNames.First] = lane.WallTimeEnabled
                    ? SnapshotLaneWallTime(lane) : null;
            }
            if (identity == own) observation.Dirty = dirty.WallTimeSnapshot(includeIo);
        }
        );
    }
}
