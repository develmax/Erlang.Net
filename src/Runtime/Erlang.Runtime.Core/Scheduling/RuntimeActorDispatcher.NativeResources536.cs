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
    {
        ArgumentNullException.ThrowIfNull(body);
        var work = new RuntimeSchedulerAuxiliary420(_ => body(), lease.Runtime536);
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

        return work.Completion;
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
