using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NativeResourcePayload524
{
    private readonly object gate = new();
    private readonly NativeResourceDomain524 domain;
    private IDisposable? typeLease;
    private nint allocation;
    private long native = 1, roots;
    private bool queued, completed;
    private readonly TaskCompletionSource retired524 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Retired524 => retired524.Task;
    internal NativeResourcePayload524? NextRetirement524;
    internal bool NormalSchedulerRetirement536;
    internal NativeResourcePayload524? NextFailure524;
    internal Exception? Failure524;
    internal Exception? DestructorFailure527;
    internal long ReferencedBytes527
    {
        get
        {
            lock (gate)
            {
                RequireLive524();

                return checked((long)((NativeResourceMagicHeader527*)allocation)->OriginalSize);
            }
        }
    }
    internal nuint SizeOfResource527
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(completed, NativeResource524Names.Retired);
                nuint original = ((NativeResourceMagicHeader527*)allocation)->OriginalSize;
                nuint metadata = (nuint)(NativeResourceMagic527.UserOffset527 - NativeResourceMagic527.BinaryBytesOffset527);

                return checked(original - metadata);
            }
        }
    }
    internal NativeResourceType524 Type { get; }
    internal nuint UserSize { get; }
    internal long AllocationBytes { get; }
    internal ProcessRuntime Runtime => Type.Runtime;
    internal nint IdentityPointer524 { get; }

    internal static long AllocationSize524(nuint userSize) => checked((long)(checked((nuint)NativeResourceMagic527.UserOffset527 + userSize)));

    internal NativeResourcePayload524(NativeResourceDomain524 domain, NativeResourceType524 type, nuint userSize)
    {
        NativeResourceMagic527.ValidateProfile527();
        this.domain = domain;
        Type = type;
        UserSize = userSize;
        nuint bytes = checked((nuint)NativeResourceMagic527.UserOffset527 + userSize);
        AllocationBytes = checked((long)bytes);
        typeLease = type.RetainPayload524();
        try
        {
            allocation = (nint)NativeMemory.Alloc(bytes);
            if (allocation == 0)
                throw new OutOfMemoryException();
            var magic = (NativeResourceMagicHeader527*)allocation;
            *magic = new NativeResourceMagicHeader527
            {
                Flags = NativeResourceMagic527.Magic,
                ApparentSize = checked((nuint)sizeof(NativeResourceHeader524) + userSize),
                RefCount = 1,
                OriginalSize = checked((nuint)(AllocationBytes - NativeResourceMagic527.BinaryBytesOffset527)),
                Destructor = NativeResourceMagic527.Destructor527,
                Allocator = (int)NativeResourceAllocator527.NativeMemory
            };
            var header = (NativeResourceHeader524*)(allocation + NativeResourceMagic527.ResourceOffset527);
            header->Type = type.Token;
            header->Monitors = 0;
            IdentityPointer524 = allocation + NativeResourceMagic527.UserOffset527;
        }
        catch
        {
            typeLease.Dispose();
            typeLease = null;
            throw;
        }
    }

    internal nint Pointer524
    {
        get
        {
            lock (gate)
            {
                RequireLive524();

                return IdentityPointer524;
            }
        }
    }

    internal void ValidateRead524()
    {
        lock (gate)
            RequireLive524();
    }

    internal void Keep524()
    {
        lock (gate)
        {
            RequireLive524();
            long next = checked(native + 1);
            _ = checked(next + roots);
            native = next;
            UpdatePhysicalCount527();
        }
    }

    internal NativeResourceRootLease524 RetainRoot524()
    {
        var lease = new NativeResourceRootLease524(this);
        lock (gate)
        {
            RequireLive524();
            long next = checked(roots + 1);
            _ = checked(native + next);
            roots = next;
            UpdatePhysicalCount527();
            lease.Admit524();
        }

        return lease;
    }

    internal void ReleaseNative524()
    {
        bool retire;
        lock (gate)
        {
            RequireLive524();
            if (native == 0)
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            native--;
            UpdatePhysicalCount527();
            retire = QueueIfDead524();
        }
        if (retire)
            domain.Enqueue524(this, runOnNormalScheduler: true);
    }

    internal void ReleaseRoot524() => ReleaseRootCore536(false);

    internal void ReleaseGuestGcRoot536() => ReleaseRootCore536(true);

    private void ReleaseRootCore536(bool guestGc)
    {
        bool retire;
        lock (gate)
        {
            if (roots == 0)
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            roots--;
            UpdatePhysicalCount527();
            retire = QueueIfDead524();
        }
        if (retire)
            domain.Enqueue524(this, guestGc);
    }

    private void UpdatePhysicalCount527()
        => ((NativeResourceMagicHeader527*)allocation)->RefCount = checked(native + roots);

    internal void InitializeNativeIdentity527(ReferenceTerm identity)
    {
        lock (gate)
        {
            RequireLive524();
            var header = (NativeResourceMagicHeader527*)allocation;
            header->Ref0 = identity.Words[0];
            header->Ref1 = identity.Words[1];
            header->Ref2 = identity.Words[2];
        }
    }

    private bool QueueIfDead524()
    {
        if (queued || native != 0 || roots != 0)
            return false;
        queued = true;

        return true;
    }

    private void RequireLive524()
    {
        ObjectDisposedException.ThrowIf(queued || completed, NativeResource524Names.Retired);
    }

    internal void AbandonUnpublished524()
    {
        nint memory;
        IDisposable? retained;
        lock (gate)
        {
            if (completed || queued || native != 1 || roots != 0)
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            completed = true;
            memory = allocation;
            allocation = 0;
            retained = typeLease;
            typeLease = null;
        }
        NativeResourceAddresses524.Forget524(this);
        try
        {
            NativeMemory.Free((void*)memory);
        }
        finally
        {
            retained?.Dispose();
        }
    }

    internal void Complete524()
    {
        Exception? failure = null;
        try
        {
            var header = (NativeResourceMagicHeader527*)allocation;
            int result = ((delegate* unmanaged[Cdecl]<nint, int>)header->Destructor)(allocation);
            failure = DestructorFailure527 ?? (result == 1 ? null : new InvalidOperationException(NativeResource527Names.BadMagic));
        }
        catch (Exception error)
        {
            failure = error;
        }
        nint memory;
        IDisposable? retained;
        lock (gate)
        {
            if (completed || !queued || native != 0 || roots != 0)
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            completed = true;
            memory = allocation;
            allocation = 0;
            retained = typeLease;
            typeLease = null;
        }
        NativeResourceAddresses524.Forget524(this);
        try
        {
            NativeMemory.Free((void*)memory);
        }
        finally
        {
            try
            {
                retained?.Dispose();
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }
        }
        Failure524 = failure;
        domain.Completed524(this);
        retired524.TrySetResult();
    }
}
