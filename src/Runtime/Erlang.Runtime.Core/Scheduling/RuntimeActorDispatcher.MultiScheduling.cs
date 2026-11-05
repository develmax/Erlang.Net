namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private bool multiSchedulingBlocked;

    internal void SetMultiSchedulingBlocked(bool blocked)
    {
        lock (gate)
        {
            if (retiring || multiSchedulingBlocked == blocked)
            {
                return;
            }
            multiSchedulingBlocked = blocked;
            // Binding is process state, not a queue assignment. Preserve it
            // while placing unbound guests on the one admitted lane. Strongly
            // bound guests on suspended lanes wait for a matching unblock.
            foreach (var scheduler in members)
            {
                if (scheduler.Running || scheduler.ReadyNode is not { } node)
                {
                    continue;
                }
                node.List!.Remove(node);
                scheduler.ReadyNode = null;
                Ready(scheduler);
            }
            StartWorkers();
        }
    }

    private bool MultiSchedulingLaneMayRun(RuntimeSchedulerLane lane)
        => !multiSchedulingBlocked || lane.Identity == ProcessSchedulerNames.First;
}
