namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private int nativeResourceOwners536;

    internal NativeResourceDispatcherLease536 AcquireNativeResourceOwner536(ProcessRuntime runtime)
    {
        if (!runtime.OwnsActorDispatcher428(this)) throw new ArgumentException(NativeResource536Names.InvalidLease);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            var lease = new NativeResourceDispatcherLease536(this, runtime);
            nativeResourceOwners536 = checked(nativeResourceOwners536 + 1);

            return lease;
        }
    }

    internal Task DispatchNativeResource536(NativeResourceDispatcherLease536 lease, Action body)
        => DispatchNativeResourceCore989(lease, body, false);

    internal Task DispatchNativeResourceWithWait989(NativeResourceDispatcherLease536 lease, Action body)
        => DispatchNativeResourceCore989(lease, body, true);

    private Task DispatchNativeResourceCore989(NativeResourceDispatcherLease536 lease, Action body, bool prepareWait)
    {
        ArgumentNullException.ThrowIfNull(body);
        var work = new RuntimeSchedulerAuxiliary420(_ => body(), lease.Runtime536);
        WaitHandle? ownedWait = null;
        Exception? admissionFailure = null;
        try
        {
            // The unique fresh task is private until admission returns. Include
            // lease rejection and queue failure in the wait-resource close path.
            if (prepareWait) ownedWait = ((IAsyncResult)work.Completion).AsyncWaitHandle;
            lock (gate)
            {
                if (!lease.IsOwnedBy536(this)) throw new ObjectDisposedException(NativeResource536Names.InvalidLease);
                // Existing admitted owner can finish true GC retirement after close.
                // One actual queue node, never an all-lanes batch/double destructor.
                RuntimeSchedulerLane lane = lanes.FirstOrDefault(candidate => candidate.Online) ?? lanes[0];
                lane.Auxiliary420.AddLast(work.Node);
                try
                {
                    StartWorkers();
                }
                catch (Exception error)
                {
                    work.Node.List?.Remove(work.Node);
                    work.Fail(error);
                    throw;
                }
            }
            // The actual caller now owns the task's prepared wait view until
            // physical completion. No other owner receives this private task.
            ownedWait = null;

            return work.Completion;
        }
        catch (Exception failure)
        {
            admissionFailure = failure;
            throw;
        }
        finally
        {
            try
            {
                ownedWait?.Dispose();
            }
            catch (Exception cleanup) when (admissionFailure is not null)
            {
                throw new AggregateException(admissionFailure, cleanup);
            }
        }
    }

    internal void ReleaseNativeResourceOwner536(NativeResourceDispatcherLease536 lease)
    {
        lock (gate)
        {
            if (!lease.IsOwnedBy536(this)) return;
            if (nativeResourceOwners536 <= 0) throw new InvalidOperationException(NativeResource536Names.InvalidLease);
            if (!lease.MarkReleased536()) return;
            nativeResourceOwners536--;
            CompleteIfIdle();
        }
    }
}
