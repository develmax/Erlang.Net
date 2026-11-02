namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    // Only outstanding real control owners are retained. No permanent actor cache.
    private readonly Dictionary<RuntimeProcessTaskScheduler, RuntimeActorControlLease428> controls428 = new();

    internal bool TryAcquireControl428(ProcessContext target, out RuntimeActorControlLease428? lease)
    {
        if (!target.Runtime.OwnsActorDispatcher428(this))
            throw new ArgumentException(RuntimeActorControlNames428.Foreign, nameof(target));
        lease = null;
        lock (gate)
        {
            var scheduler = target.ActorTaskScheduler;
            if (normalBarrier696 is not null || scheduler is null || scheduler.Running || controls428.ContainsKey(scheduler)) return false;
            // Both potentially allocating steps precede queue removal/publication.
            var candidate = new RuntimeActorControlLease428(this, scheduler);
            controls428.Add(scheduler, candidate);
            if (scheduler.ReadyNode is { } node)
            {
                node.List!.Remove(node);
                scheduler.ReadyNode = null;
            }
            lease = candidate;

            return true;
        }
    }

    internal void ReleaseControl428(RuntimeActorControlLease428 lease)
    {
        lock (gate)
        {
            var scheduler = lease.Scheduler;
            if (!controls428.TryGetValue(scheduler, out var current) || !ReferenceEquals(current, lease))
                throw new InvalidOperationException(RuntimeActorControlNames428.Missing);
            controls428.Remove(scheduler);
            AdvanceNormalBarrier696();
            if (!scheduler.Running && scheduler.ReadyNode is null && scheduler.WorkCount428 != 0)
                Ready(scheduler);
            StartWorkers();
            CompleteIfIdle();
        }
    }

    internal bool IsNormalTurnRunning428(ProcessContext target)
    {
        if (!target.Runtime.OwnsActorDispatcher428(this))
            throw new ArgumentException(RuntimeActorControlNames428.Foreign, nameof(target));
        lock (gate) return target.ActorTaskScheduler?.Running == true;
    }

}
