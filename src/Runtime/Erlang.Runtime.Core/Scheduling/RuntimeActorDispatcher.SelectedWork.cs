namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private void SelectNormalAuxiliary(RuntimeSchedulerLane lane,
        LinkedListNode<RuntimeSchedulerAuxiliary420> node, RuntimeNormalWorkSelection selection)
    {
        selection.Acquire(node.Value);
        lane.Auxiliary420.Remove(node);
        lane.AuxiliaryRunning420 = true;
        DriverAuxiliarySelected422(lane, true);
        WallTimeBecameActive(lane);
        selection.WallTimeStarted = true;
    }

    // Called exactly once under D, even if selection or execution setup failed.
    private void FinalizeNormalSelection(RuntimeNormalWorkSelection selection, Exception? failure)
    {
        if (!selection.BeginFinalize()) return;
        RuntimeSchedulerLane lane = selection.Lane;
        if (selection.Scheduler is null && selection.Auxiliary is null) return;
        try
        {
            selection.RestoreUnexecuted();
            if (selection.Auxiliary is { } auxiliary && !selection.ExecutionConsumed)
            {
                auxiliary.Fail(failure!);
                selection.Consumed();
            }
            if (selection.WallTimeStarted) WallTimeBecameIdle(lane);
        }
        finally
        {
            if (selection.Auxiliary is not null) lane.AuxiliaryRunning420 = false;
            if (selection.Scheduler is { } scheduler)
            {
                scheduler.Running = false;
                lane.Running = null;
                if (!selection.ExecutionConsumed)
                    scheduler.LastExecutedEpoch = selection.PreviousExecutedEpoch;
                AssignLane(scheduler);
                if (scheduler.WorkCount428 != 0)
                {
                    // An unexecuted work item regains its original head/node
                    // only if cancellation/topology/priority did not change its
                    // admission. Otherwise use the original cleanup/Ready rules.
                    var target = scheduler.Owner.Cancellation.IsCancellationRequested
                        && (scheduler.Lane.Online || retiring) ? scheduler.Lane.Cleanup
                        : scheduler.Lane.Queue(scheduler.Owner.DispatchPriority428);
                    if (!selection.ExecutionConsumed && !controls428.ContainsKey(scheduler)
                        && ReferenceEquals(scheduler.Lane, lane)
                        && ReferenceEquals(target, selection.OriginalReadyQueue))
                    {
                        var node = selection.OriginalReadyNode!;
                        target.AddFirst(node);
                        scheduler.ReadyNode = node;
                    }
                    else Ready(scheduler);
                }
            }
        }
    }
}
