using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

// Actual native loader-state storage. Consumer wiring is a separate mandatory
// slice: an unused allocation is never native Guest ownership evidence.
internal sealed class PreparedLoaderOwner807 : IThreadPoolWorkItem
{
    private static long liveAllocations807;
    internal static bool AnyLive807 => Volatile.Read(ref liveAllocations807) != 0;
    internal long ActualAllocationBytes807 => Volatile.Read(ref allocation) == 0 ? 0 : PreparedLoaderNames807.MagicPrefixBytes + PreparedLoaderNames807.PointerBytes;
    private readonly object gate = new();
    private readonly ModuleRegistry registry;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource physical = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<PreparedLoaderCloseDisposition807> shutdownClose = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task<PreparedLoaderCloseDisposition807> ShutdownClose807 => shutdownClose.Task;
    internal Task WaitPhysical807 => JoinPhysical807();

    private async Task JoinPhysical807()
    {
        await ready.Task.ConfigureAwait(false);
        Execute();
        await physical.Task.ConfigureAwait(false);
    }

    internal bool TryReadShutdownClose807(out PreparedLoaderCloseDisposition807? disposition)
    {
        disposition = shutdownClose.Task.IsCompletedSuccessfully ? shutdownClose.Task.Result : null;

        return disposition is not null;
    }

    internal void InstallShutdownClose807(PreparedLoaderCloseDisposition807 disposition)
    {
        if (!ReferenceEquals(disposition.Owner807, this) || !ReferenceEquals(disposition.Image807.Owner, registry))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        registry.RequirePreparedShutdownBinding807(disposition.Image807, this);
        shutdownClose.TrySetResult(disposition);
    }

    private nint allocation;
    private bool producer = true, identityBound, closing, scheduled;
    private long guest, host, operations;
    private int executing, queueSettled, closeAttempted;
    private Exception? queueFailure, destructorFailure;
    internal ProcessRuntime Runtime807 { get; }
    internal Task PhysicalRetired807 => physical.Task;
    internal Task Settled807 => settled.Task;

    private static unsafe nint Destructor807 => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&Destroy807;

    internal unsafe PreparedLoaderOwner807(
        ModuleRegistry registry,
        ProcessRuntime runtime,
        PreparedBeamImage image,
        PreparedLoaderGrant807 grant
    )
    {
        this.registry = registry;
        Runtime807 = runtime;
        if (!ReferenceEquals(image.Owner, registry) || !ReferenceEquals(runtime.Modules, registry))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        if (IntPtr.Size != PreparedLoaderNames807.PointerBytes
            || sizeof(NativeResourceMagicHeader527) != PreparedLoaderNames807.MagicPrefixBytes)
            throw new PlatformNotSupportedException(PreparedLoaderNames807.Invalid);
        nuint bytes = checked((nuint)(sizeof(NativeResourceMagicHeader527) + sizeof(nint)));
        ModuleLiteralLease800? literals = registry.ClaimPreparedLoaderLiteral807(grant, image);
        GCHandle state = default;
        nint memory = 0;
        try
        {
            state = GCHandle.Alloc(new PreparedLoaderNativeState807(this, image, literals));
            memory = (nint)NativeMemory.AllocZeroed(bytes);
            if (memory == 0) throw new OutOfMemoryException();
            var header = (NativeResourceMagicHeader527*)memory;
            header->Flags = NativeResourceMagic527.Magic;
            header->ApparentSize = (nuint)sizeof(nint);
            header->RefCount = 1;
            header->OriginalSize = bytes - (nuint)NativeResourceMagic527.BinaryBytesOffset527;
            header->Destructor = Destructor807;
            header->Allocator = (int)NativeResourceAllocator527.NativeMemory;
            *(nint*)(memory + sizeof(NativeResourceMagicHeader527)) = GCHandle.ToIntPtr(state);
            allocation = memory;
            Interlocked.Increment(ref liveAllocations807);
        }
        catch
        {
            NativeMemory.Free((void*)memory);
            if (state.IsAllocated) state.Free();
            literals?.Dispose();
            throw;
        }
    }

    internal unsafe void BindIdentity807(IReadOnlyList<uint> words)
    {
        lock (gate)
        {
            RequireLive807();
            if (identityBound || !producer || words.Count != PreparedLoaderNames807.IdentityWords
                || (words[1] & ReferenceNumberLayout.MagicMarker) == 0)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            var header = (NativeResourceMagicHeader527*)allocation;
            header->Ref0 = words[0];
            header->Ref1 = words[1];
            header->Ref2 = words[2];
            identityBound = true;
        }
    }

    internal unsafe PreparedLoaderLease807 Retain807(PreparedLoaderLeaseKind807 kind)
    {
        var lease = new PreparedLoaderLease807(this, kind);
        lock (gate)
        {
            RequireLive807();
            ObjectDisposedException.ThrowIf(closing || scheduled, PreparedLoaderNames807.Closed);
            if (!identityBound && kind != PreparedLoaderLeaseKind807.Operation)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            long nextGuest = guest, nextHost = host, nextOperations = operations;
            if (kind == PreparedLoaderLeaseKind807.Guest) nextGuest = checked(nextGuest + 1);
            else if (kind == PreparedLoaderLeaseKind807.Host) nextHost = checked(nextHost + 1);
            else if (kind == PreparedLoaderLeaseKind807.Operation) nextOperations = checked(nextOperations + 1);
            else throw new ArgumentOutOfRangeException(nameof(kind));
            long refs = checked((producer ? 1 : 0) + nextGuest + nextHost + nextOperations);
            ((NativeResourceMagicHeader527*)allocation)->RefCount = refs;
            guest = nextGuest;
            host = nextHost;
            operations = nextOperations;
        }

        return lease;
    }

    internal PreparedBeamImage ReadImage807(PreparedLoaderLease807 lease)
    {
        lock (gate)
        {
            RequireLease807(lease);

            return ReadState807().Image807;
        }
    }

    internal nint ReadBinary807(PreparedLoaderLease807 lease)
    {
        lock (gate)
        {
            RequireLease807(lease);

            return allocation;
        }
    }

    internal unsafe IReadOnlyList<uint> ReadNumbers807(PreparedLoaderLease807 lease)
    {
        lock (gate)
        {
            RequireLease807(lease);
            if (!identityBound) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            var header = (NativeResourceMagicHeader527*)allocation;

            return [header->Ref0, header->Ref1, header->Ref2];
        }
    }

    internal void CompleteHandoff807()
    {
        lock (gate)
        {
            RequireLive807();
            if (!identityBound || guest == 0 && host == 0)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            if (!producer) return;
            producer = false;
            WriteRefs807();
        }
    }

    // All releases are metadata/native-count updates plus asynchronous enqueue.
    // They never call image/module/domain close from the caller's H/M/R stack.
    internal void ReleaseProducer807()
    {
        bool queue;
        lock (gate)
        {
            if (!producer) return;
            producer = false;
            WriteRefs807();
            queue = MakeReady807();
        }
        if (queue) Schedule807();
    }

    internal void Release807(PreparedLoaderLeaseKind807 kind)
    {
        bool queue;
        lock (gate)
        {
            if (kind == PreparedLoaderLeaseKind807.Guest && guest > 0) guest--;
            else if (kind == PreparedLoaderLeaseKind807.Host && host > 0) host--;
            else if (kind == PreparedLoaderLeaseKind807.Operation && operations > 0) operations--;
            else throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            WriteRefs807();
            queue = MakeReady807();
        }
        if (queue) Schedule807();
    }

    internal void CloseVm807()
    {
        bool queue;
        lock (gate)
        {
            closing = true;
            producer = false;
            WriteRefs807();
            queue = MakeReady807();
        }
        if (queue) Schedule807();
    }

    private bool MakeReady807()
    {
        if (scheduled || producer || guest != 0 || operations != 0 || !closing && host != 0) return false;
        scheduled = true;
        ready.TrySetResult();

        return true;
    }

    private unsafe void WriteRefs807()
    {
        if (allocation != 0)
            ((NativeResourceMagicHeader527*)allocation)->RefCount = checked((producer ? 1 : 0) + guest + host + operations);
    }

    private void Schedule807()
    {
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false))
                throw new InvalidOperationException(PreparedLoaderNames807.QueueFailure);
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

    internal async Task Join807()
    {
        await ready.Task.ConfigureAwait(false);
        Execute();
        await settled.Task.ConfigureAwait(false);
    }

    public void Execute()
    {
        lock (gate) if (!scheduled) return;
        var spin = new SpinWait();
        while (Volatile.Read(ref queueSettled) == 0) spin.SpinOnce();
        if (Interlocked.CompareExchange(ref executing, 1, 0) == 0) _ = Retire807();
    }

    private async Task Retire807()
    {
        Exception? failure = Volatile.Read(ref queueFailure);
        nint memory = 0;
        PreparedBeamImage? image = null;
        try
        {
            lock (gate)
            {
                memory = allocation;
                image = ReadState807().Image807;
            }
            PreparedLoaderCloseDisposition807 disposition = await registry.ResolvePreparedLoaderClose807(image, this).ConfigureAwait(false);
            ReleasePreparedLiterals807(image);
            await disposition.CodeClose807.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = Combine807(failure, error);
        }
        try
        {
            if (image is not null) ReleasePreparedLiterals807(image);
        }
        catch (Exception error)
        {
            failure = Combine807(failure, error);
        }
        Volatile.Write(ref closeAttempted, 1);
        bool freed = false;
        try
        {
            if (image is null) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            DestroyAndFree807(memory);
            freed = true;
        }
        catch (Exception error)
        {
            failure = Combine807(failure, error);
        }
        if (failure is null) physical.TrySetResult();
        else physical.TrySetException(failure);
        try
        {
            registry.PreparedLoaderSettled807(
                image,
                this,
                freed,
                failure
            );
        }
        catch (Exception error)
        {
            failure = Combine807(failure, error);
        }
        if (failure is null) settled.TrySetResult();
        else settled.TrySetException(failure);
    }

    internal void ReleasePreparedLiterals807(PreparedBeamImage image)
    {
        PreparedLoaderNativeState807 state;
        lock (gate)
        {
            state = ReadState807();
            if (!ReferenceEquals(state.Image807, image) || !scheduled && operations == 0)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        }
        state.ReleaseLiterals807();
    }

    private unsafe void DestroyAndFree807(nint memory)
    {
        int destroyed = ((delegate* unmanaged[Cdecl]<nint, int>)Destructor807)(memory);
        if (destroyed != 1)
            throw Volatile.Read(ref destructorFailure) ?? new InvalidOperationException(PreparedLoaderNames807.DestructorFailure);
        lock (gate) allocation = 0;
        NativeMemory.Free((void*)memory);
        Interlocked.Decrement(ref liveAllocations807);
    }

    // A losing/unpublished allocation never acquired the raw image's producer.
    // Free only its actual native payload; do not retire the shared image.
    internal void AbortUnregistered807()
    {
        nint memory;
        lock (gate)
        {
            if (allocation == 0) return;
            RequireLive807();
            if (!producer || identityBound || scheduled || guest != 0 || host != 0 || operations != 0)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            producer = false;
            WriteRefs807();
            scheduled = true;
            memory = allocation;
            ready.TrySetResult();
            Volatile.Write(ref executing, 1);
            Volatile.Write(ref queueSettled, 1);
        }
        try
        {
            PreparedLoaderNativeState807 state;
            lock (gate) state = ReadState807();
            state.ReleaseLiterals807();
            Volatile.Write(ref closeAttempted, 1);
            DestroyAndFree807(memory);
            physical.TrySetResult();
            settled.TrySetResult();
        }
        catch (Exception error)
        {
            physical.TrySetException(error);
            settled.TrySetException(error);
            throw;
        }
    }

    private unsafe PreparedLoaderNativeState807 ReadState807()
    {
        RequireLive807();
        nint handle = *(nint*)(allocation + sizeof(NativeResourceMagicHeader527));
        if (handle == 0 || GCHandle.FromIntPtr(handle).Target is not PreparedLoaderNativeState807 state
            || !ReferenceEquals(state.Owner807, this))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);

        return state;
    }

    private unsafe void RequireLive807()
    {
        ObjectDisposedException.ThrowIf(allocation == 0, PreparedLoaderNames807.Closed);
        var header = (NativeResourceMagicHeader527*)allocation;
        if (header->Flags != NativeResourceMagic527.Magic || header->Destructor != Destructor807
            || header->ApparentSize != (nuint)sizeof(nint)
            || header->OriginalSize != (nuint)(sizeof(NativeResourceMagicHeader527) + sizeof(nint) - NativeResourceMagic527.BinaryBytesOffset527)
            || header->Allocator != (int)NativeResourceAllocator527.NativeMemory
            || header->RefCount != checked((producer ? 1 : 0) + guest + host + operations))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
    }

    private void RequireLease807(PreparedLoaderLease807 lease)
    {
        RequireLive807();
        ObjectDisposedException.ThrowIf(!lease.Owns807(this)
            || closing && lease.Kind807 == PreparedLoaderLeaseKind807.Host, PreparedLoaderNames807.Closed);
    }

    private static Exception Combine807(Exception? first, Exception second)
        => first is null ? second : new AggregateException(first, second);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Destroy807(nint memory)
    {
        PreparedLoaderOwner807? owner = null;
        try
        {
            nint* payload = (nint*)(memory + sizeof(NativeResourceMagicHeader527));
            GCHandle handle = GCHandle.FromIntPtr(*payload);
            if (handle.Target is not PreparedLoaderNativeState807 state)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            owner = state.Owner807;
            if (Volatile.Read(ref owner.closeAttempted) == 0 || Volatile.Read(ref owner.allocation) != memory || state.HasLiteralLease807)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            *payload = 0;
            handle.Free();

            return 1;
        }
        catch (Exception error)
        {
            if (owner is not null) Interlocked.CompareExchange(ref owner.destructorFailure, error, null);

            return 0;
        }
    }
}
