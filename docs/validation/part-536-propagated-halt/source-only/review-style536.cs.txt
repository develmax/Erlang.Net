namespace Erlang;

// A program is real BM/AC memory; no finalizer owns a Guest reference.
internal sealed class BinaryProgramBacking598 : IThreadPoolWorkItem
{
    private readonly object gate = new();
    private readonly BinaryProgramOwner598 owner;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long guest, host;
    private int readers, executing, queueSettled;
    private bool producer = true, identityBound, closing, scheduled, freed;
    private Exception? queueFailure;
    internal BinaryProgramBacking598? NextFailure598;
    internal BinaryProgramArena598 Arena598 { get; }
    internal ProcessRuntime Runtime598 => owner.Runtime598;
    internal Task Retired598 => retired.Task;
    internal bool PhysicalFreed598
    {
        get
        {
            lock (gate) return freed;
        }
    }

    internal BinaryProgramBacking598(BinaryProgramOwner598 owner, BinaryProgramArena598 arena)
    {
        this.owner = owner;
        Arena598 = arena;
    }

    internal BinaryProgramLease598 Retain598(BinaryProgramLeaseKind598 kind) => owner.Retain598(this, kind);

    internal BinaryArenaReader598 Read598() => owner.Reader598(this);

    internal void BindIdentity598(IReadOnlyList<uint> numbers)
    {
        lock (gate)
        {
            RequireLive598();
            if (identityBound || !producer) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            Arena598.BindNumbers598(numbers);
            identityBound = true;
        }
    }

    // Owner gate is held. No owner calls, callbacks or graph walks here.
    internal void RetainCore598(BinaryProgramLeaseKind598 kind)
    {
        lock (gate)
        {
            RequireLive598();
            ObjectDisposedException.ThrowIf(closing || scheduled || !identityBound, BinaryArenaNames598.Closed);
            long nextGuest = guest, nextHost = host;
            if (kind == BinaryProgramLeaseKind598.Guest) nextGuest = checked(nextGuest + 1);
            else if (kind == BinaryProgramLeaseKind598.Host) nextHost = checked(nextHost + 1);
            else throw new ArgumentOutOfRangeException(nameof(kind));
            Arena598.WritePhysicalRefs598(checked(nextGuest + nextHost));
            guest = nextGuest;
            host = nextHost;
        }
    }

    internal void RetainReaderCore598()
    {
        lock (gate)
        {
            RequireLive598();
            ObjectDisposedException.ThrowIf(closing || scheduled || (!producer && guest == 0 && host == 0), BinaryArenaNames598.Closed);
            readers = checked(readers + 1);
        }
    }

    internal nint Binary598(BinaryProgramLease598 lease)
    {
        lock (gate)
        {
            RequireLease598(lease);

            return Arena598.Binary598;
        }
    }

    internal IReadOnlyList<uint> Numbers598(BinaryProgramLease598 lease)
    {
        lock (gate)
        {
            RequireLease598(lease);

            return Arena598.ReadNumbers598();
        }
    }

    internal void Release598(BinaryProgramLeaseKind598 kind)
    {
        bool queue;
        lock (gate)
        {
            if (kind == BinaryProgramLeaseKind598.Guest)
            {
                if (guest == 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
                guest--;
            }
            else if (kind == BinaryProgramLeaseKind598.Host)
            {
                if (host == 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
                host--;
            }
            else throw new ArgumentOutOfRangeException(nameof(kind));
            // VM close may invalidate a late Host without touching freed memory.
            if (Arena598.IsAllocated598) Arena598.WritePhysicalRefs598(checked(guest + host));
            queue = MakeReady598();
        }
        if (queue) Schedule598();
    }

    internal void ReleaseReader598()
    {
        bool queue;
        lock (gate)
        {
            if (readers == 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            readers--;
            queue = MakeReady598();
        }
        if (queue) Schedule598();
    }

    internal void ReleaseProducer598()
    {
        bool queue;
        lock (gate)
        {
            if (!producer) return;
            producer = false;
            queue = MakeReady598();
        }
        if (queue) Schedule598();
    }

    internal void CloseVm598()
    {
        bool queue;
        lock (gate)
        {
            closing = true;
            queue = MakeReady598();
        }
        if (queue) Schedule598();
    }

    private bool MakeReady598()
    {
        if (scheduled || freed || producer || readers != 0 || guest != 0 || (!closing && host != 0)) return false;
        scheduled = true;
        ready.TrySetResult();

        return true;
    }

    private void Schedule598()
    {
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false))
                throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref queueFailure, error, null);
        }
        finally
        {
            Volatile.Write(ref queueSettled, 1);
        }
    }

    internal async Task Join598()
    {
        await ready.Task.ConfigureAwait(false);
        Execute(); // Per-allocation fallback, never a global owner wait.
        await retired.Task.ConfigureAwait(false);
    }

    public void Execute()
    {
        lock (gate)
        {
            if (!scheduled) return;
        }
        var spin = new SpinWait();
        while (Volatile.Read(ref queueSettled) == 0) spin.SpinOnce();
        if (Interlocked.CompareExchange(ref executing, 1, 0) != 0) return;
        Exception? failure = null;
        try
        {
            owner.Dispatch598(Free598).GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            failure = error;
        }
        Exception? queued = Volatile.Read(ref queueFailure);
        if (queued is not null) failure = Combine598(queued, failure);
        try
        {
            if (PhysicalFreed598) owner.Finished598(this, failure);
        }
        catch (Exception error)
        {
            failure = Combine598(error, failure);
        }
        if (failure is null)
        {
            retired.TrySetResult();
        }
        else
        {
            retired.TrySetException(failure);
        }
    }

    private void Free598()
    {
        nint allocation;
        lock (gate)
        {
            RequireLive598();
            if (!scheduled || producer || guest != 0 || readers != 0 || (!closing && host != 0))
                throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            // Detach atomically before permitting any late Host release.
            allocation = Arena598.DetachForRetirement598();
        }
        Arena598.FreeDetachedAllocation598(allocation);
        lock (gate) freed = true;
    }

    private void RequireLive598()
    {
        ObjectDisposedException.ThrowIf(freed || !Arena598.IsAllocated598, BinaryArenaNames598.Closed);
        Arena598.ValidatePhysicalRefs598(checked(guest + host));
    }

    private void RequireLease598(BinaryProgramLease598 lease)
    {
        if (!lease.Owns598(this)) throw new ObjectDisposedException(nameof(BinaryProgramLease598));
        RequireLive598();
        if (!identityBound) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
    }

    private static Exception Combine598(Exception first, Exception? second)
    {
        if (second is null) return first;
        try
        {
            return new AggregateException(first, second);
        }
        catch
        {
            return first;
        }
    }
}
