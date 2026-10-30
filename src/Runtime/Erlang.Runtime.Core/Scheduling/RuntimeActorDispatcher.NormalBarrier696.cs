namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private readonly LinkedList<RuntimeNormalBarrierRequest696> normalRequests696 = new();
    private RuntimeNormalBarrierRequest696? normalBarrier696;

    internal async ValueTask<RuntimeNormalBarrierRequest696> AcquireNormalBarrier696(ProcessContext requester)
    {
        if (!requester.Runtime.OwnsActorDispatcher428(this)
            || requester.ActorTaskScheduler is not { } scheduler
            || !RuntimeActorExecutionTurn.IsOwnedBy(requester)
            || DirtyNativeThread419.Owner is not null)
            throw new InvalidOperationException(RuntimeNormalBarrierNames696.Requester);
        var request = new RuntimeNormalBarrierRequest696(this, scheduler);
        using var registration = requester.Cancellation.UnsafeRegister(
            static state => ((RuntimeNormalBarrierRequest696)state!).Cancel696(), request);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            requester.Cancellation.ThrowIfCancellationRequested();
            foreach (var pending in normalRequests696)
                if (ReferenceEquals(pending.Scheduler696, scheduler))
                    throw new InvalidOperationException(RuntimeNormalBarrierNames696.Reentrant);
            normalRequests696.AddLast(request.Node696);
            AdvanceNormalBarrier696();
        }
        // A raw Task.ConfigureAwait(false) would run the requester outside its
        // actor lane. Reentry here uses the existing actual actor scheduler.
        await new ProcessExecutionAwaitable(
            new ValueTask(request.Ready696.Task), scheduler, ProcessExecutionAdmission.Reentry);

        return request;
    }

    // All state below is protected by the existing dispatcher gate.
    private void AdvanceNormalBarrier696()
    {
        if (normalBarrier696 is null && normalRequests696.First is { } first)
        {
            // Finish an already-admitted topology change first. In particular,
            // never freeze all actors behind a requester bound to an offline lane.
            var request = first.Value;
            if (onlineTurn is not null || onlineChange is not null || !request.Scheduler696.Lane.Online)
                return;
            normalBarrier696 = request;
        }
        if (normalBarrier696 is not { Granted696: false } pending)
            return;
        if (controls428.Count != 0 || normalNativeActive697 != 0) return;
        foreach (var lane in lanes)
            if (lane.AuxiliaryRunning420
                || lane.Running is { } running && !ReferenceEquals(running, pending.Scheduler696))
                return;
        pending.Granted696 = true;
        pending.Ready696.TrySetResult();
    }

    private bool NormalBarrierLaneMayStart696(RuntimeSchedulerLane lane)
        => normalBarrier696 is not { } pending
            || ReferenceEquals(pending.Scheduler696.Lane, lane)
                && !pending.Scheduler696.Running
                && pending.Scheduler696.ReadyNode is not null;

    private RuntimeProcessTaskScheduler? SelectBarrierRequester696(RuntimeSchedulerLane lane)
    {
        var scheduler = normalBarrier696!.Scheduler696;
        if (!lane.Online || !ReferenceEquals(scheduler.Lane, lane)
            || scheduler.Running || scheduler.ReadyNode is not { } node)
            return null;
        // Do not strand the sole admitted requester behind another actor's
        // higher-priority head. Keep every other node in its original queue.
        node.List!.Remove(node);
        scheduler.ReadyNode = null;
        scheduler.Running = true;
        lane.Running = scheduler;
        WallTimeBecameActive(lane);

        return scheduler;
    }

    internal void CancelNormalBarrier696(RuntimeNormalBarrierRequest696 request)
    {
        lock (gate)
        {
            // A granted lease protects a live synchronous native transition.
            // Cancellation after grant never releases it underneath the callback.
            if (request.Granted696 || request.Node696.List is null)
                return;
            var cancellation = request.Scheduler696.Owner.Cancellation;
            normalRequests696.Remove(request.Node696);
            if (ReferenceEquals(normalBarrier696, request))
                normalBarrier696 = null;
            request.Detach696();
            request.Ready696.TrySetCanceled(cancellation);
            AdvanceNormalBarrier696();
            GrantOnlineTurn();
            StartWorkers();
        }
    }

    internal void ReleaseNormalBarrier696(RuntimeNormalBarrierRequest696 request)
    {
        lock (gate)
        {
            if (request.Node696.List is null)
                return;
            if (!ReferenceEquals(normalBarrier696, request) || !request.Granted696)
                throw new InvalidOperationException(RuntimeNormalBarrierNames696.Lease);
            normalRequests696.Remove(request.Node696);
            normalBarrier696 = null;
            request.Detach696();
            WakeNormalNative697();
            AdvanceNormalBarrier696();
            GrantOnlineTurn();
            StartWorkers();
            CompleteIfIdle();
        }
    }

    private void RetirePendingNormalBarriers696(Exception failure)
    {
        var node = normalRequests696.First;
        while (node is not null)
        {
            var next = node.Next;
            var request = node.Value;
            // A live granted callback keeps its real lease until finally.
            if (!request.Granted696)
            {
                normalRequests696.Remove(node);
                if (ReferenceEquals(normalBarrier696, request))
                    normalBarrier696 = null;
                request.Detach696();
                request.Ready696.TrySetException(failure);
            }
            node = next;
        }
    }
}
