namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private readonly LinkedList<RuntimeNormalNativeRequest697> normalNativeQueue697 = new();
    private readonly Exception normalNativeClosedFailure697 = new ObjectDisposedException(RuntimeNormalNativeNames697.Closed);
    private int normalNativeActive697;
    private bool normalNativeClosing697;

    internal IDisposable EnterNormalNative697(ProcessContext context)
    {
        if (!context.Runtime.OwnsActorDispatcher428(this))
            throw new InvalidOperationException(RuntimeNormalNativeNames697.Foreign);
        lock (gate)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (normalNativeClosing697 || retiring)
                throw normalNativeClosedFailure697;
            var scheduler = context.ActorTaskScheduler;
            bool actualActor = RuntimeActorExecutionTurn.IsOwnedBy(context)
                && DirtyNativeThread419.Owner is null
                && scheduler is not null && ReferenceEquals(scheduler.Owner, context)
                && scheduler.Running && ReferenceEquals(scheduler.Lane.Running, scheduler);
            if (actualActor)
            {
                if (normalBarrier696 is { Granted696: true } barrier
                    && !ReferenceEquals(barrier.Scheduler696, scheduler))
                    throw new InvalidOperationException(RuntimeNormalNativeNames697.Actor);

                // This whole synchronous call is already covered by Running.
                return RuntimeNormalNativeActorScope697.Instance;
            }
        }
        var request = new RuntimeNormalNativeRequest697(this, context);
        bool returned = false;
        using var registration = context.Cancellation.UnsafeRegister(
            static state => ((RuntimeNormalNativeRequest697)state!).Cancel697(), request);
        try
        {
            lock (gate)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (normalNativeClosing697 || retiring)
                    throw normalNativeClosedFailure697;
                if (normalBarrier696 is { Granted696: true })
                    normalNativeQueue697.AddLast(request.Node697);
                else
                    AdmitNormalNative697(request);
            }
            // Cancellation is decided under gate before grant. A token-aware
            // Wait could throw after grant and lose an already-counted lease.
            request.Ready697.Task.GetAwaiter().GetResult();
            returned = true;

            return request;
        }
        finally
        {
            if (!returned) request.Dispose();
        }
    }

    private void AdmitNormalNative697(RuntimeNormalNativeRequest697 request)
    {
        // Pending is deliberately not Granted: an admitted auxiliary may await
        // this off-turn continuation. Counting lets it finish without bypass.
        normalNativeActive697 = checked(normalNativeActive697 + 1);
        request.Active697 = true;
        request.Ready697.TrySetResult();
    }

    private void WakeNormalNative697()
    {
        if (normalBarrier696 is { Granted696: true }) return;
        while (normalNativeQueue697.First is { } node)
        {
            var request = node.Value;
            normalNativeQueue697.Remove(node);
            if (normalNativeClosing697 || retiring)
            {
                request.Detach697();
                request.Ready697.TrySetException(normalNativeClosedFailure697);
                continue;
            }
            var cancellation = request.Context697.Cancellation;
            if (cancellation.IsCancellationRequested)
            {
                request.Detach697();
                request.Ready697.TrySetCanceled(cancellation);
                continue;
            }
            try
            {
                AdmitNormalNative697(request);
            }
            catch (Exception failure) when (!request.Active697)
            {
                // Do not abandon independent waiters if one admission fails.
                // Never discard an already-admitted physical invocation.
                request.Detach697();
                request.Ready697.TrySetException(failure);
            }
        }
    }

    internal void CancelNormalNative697(RuntimeNormalNativeRequest697 request)
    {
        lock (gate)
        {
            if (request.Active697 || request.Node697.List is null) return;
            var cancellation = request.Context697.Cancellation;
            normalNativeQueue697.Remove(request.Node697);
            request.Detach697();
            request.Ready697.TrySetCanceled(cancellation);
        }
    }

    internal void ReleaseNormalNative697(RuntimeNormalNativeRequest697 request)
    {
        lock (gate)
        {
            if (request.Node697.List is not null)
            {
                normalNativeQueue697.Remove(request.Node697);
                request.Detach697();

                return;
            }
            if (request.Active697)
            {
                if (normalNativeActive697 <= 0)
                    throw new InvalidOperationException(RuntimeNormalNativeNames697.Count);
                normalNativeActive697--;
                request.Active697 = false;
            }
            request.Detach697();
            AdvanceNormalBarrier696();
            StartWorkers();
            CompleteIfIdle();
        }
    }

    internal void CloseNormalNative697()
    {
        lock (gate)
        {
            CloseNormalNativeUnderGate697();
            CompleteIfIdle();
        }
    }

    private void CloseNormalNativeUnderGate697()
    {
        normalNativeClosing697 = true;
        while (normalNativeQueue697.First is { } node)
        {
            var request = node.Value;
            normalNativeQueue697.Remove(node);
            request.Detach697();
            request.Ready697.TrySetException(normalNativeClosedFailure697);
        }
    }
}
