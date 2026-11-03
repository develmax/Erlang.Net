using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class NativeResourceType524 : IDisposable
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private NativeResourceCallbackVersion527 current;
    private NativeResourceTypeTable527? table;
    private GCHandle handle;
    private long payloads, pending;
    private bool registered, ownerRetired;
    internal string Name { get; }
    internal string Module527 { get; }
    internal nint Token => GCHandle.ToIntPtr(handle);
    internal ProcessRuntime Runtime => runtime;
    // Resource-name publication/unlink serializes this read independently of
    // executing native callbacks; it never waits on a callback's type lock.
    internal ModuleCodeImage CurrentImageForNames529 => Volatile.Read(ref current).Image527.Image;
    internal ModuleCodeImage CurrentImage527
    {
        get
        {
            lock (gate) return current.Image527.Image;
        }
    }

    internal NativeResourceType524(
        string name,
        NifNativeResourceImage524 image,
        nint destructor,
        nint stop,
        nint down,
        nint dynamicCall
    )
    {
        Name = name;
        Module527 = image.Image.Module;
        runtime = image.Runtime;
        current = new(image, new(
            destructor,
            stop,
            down,
            dynamicCall
        ));
        handle = GCHandle.Alloc(this);
        try
        {
            NativeResourceTypes527.Register527(this);
        }
        catch
        {
            handle.Free();
            throw;
        }
    }

    internal void RetainPending527()
    {
        lock (gate)
        {
            if (!handle.IsAllocated) throw new ObjectDisposedException(NativeResource524Names.Retired);
            pending = checked(pending + 1);
        }
    }

    internal void ReleasePending527()
    {
        NativeResourceTypeTable527? owner;
        lock (gate)
        {
            if (pending == 0) throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            pending--;
            owner = ownerRetired && pending == 0 && payloads == 0 ? table : null;
        }
        owner?.TryRetireEmpty527(this);
    }

    internal NativeResourceCallbackVersion527 Install527(NativeResourceTypeTable527 owner,
        NativeResourceCallbackVersion527 version)
    {
        lock (gate)
        {
            NativeResourceCallbackVersion527 previous = current;
            current = version;
            table = owner;
            registered = true;
            ownerRetired = false;

            return previous;
        }
    }

    internal void MarkImageRetired527()
    {
        lock (gate) ownerRetired = true;
    }

    internal bool DetachIfEmpty527(out NativeResourceCallbackVersion527? release)
    {
        lock (gate)
        {
            release = null;
            if (!registered || !ownerRetired || payloads != 0 || pending != 0) return false;
            registered = false;
            release = current;

            return true;
        }
    }

    internal NativeResourceAllocationEpoch527 EnterAllocationEpoch527() => new(table);

    internal IDisposable RetainPayload524()
    {
        var lease = new NativeResourceTypeLease524(this);
        NativeResourceTypeTable527? owner = table;
        owner?.EnterRead527();
        try
        {
            lock (gate)
            {
                if (!handle.IsAllocated || ownerRetired && !registered)
                    throw new ObjectDisposedException(NativeResource524Names.Retired);
                if (pending != 0 && !registered)
                    throw new NotSupportedException(NativeResource527Names.AllocateDuringLoad);
                if (current.Callbacks527.Down != 0)
                    throw new NotSupportedException(NativeResource527Names.MonitorStorage);
                long next = checked(payloads + 1);
                current.RequireReady527();
                payloads = next;
                lease.Admit524();
            }

            return lease;
        }
        finally
        {
            owner?.LeaveRead527();
        }
    }

    internal void ReleasePayload524()
    {
        NativeResourceCallbackVersion527? release = null;
        NativeResourceTypeTable527? owner;
        lock (gate)
        {
            if (payloads == 0) throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            payloads--;
            owner = ownerRetired && payloads == 0 && pending == 0 ? table : null;
            if (!registered && payloads == 0) release = current;
        }
        owner?.TryRetireEmpty527(this);
        release?.Dispose();
    }

    internal bool MarkImageRetired529(ModuleCodeImage image)
    {
        lock (gate)
        {
            if (!ReferenceEquals(current.Image527.Image, image)) return false;
            ownerRetired = true;

            return true;
        }
    }

    private (NativeResourceCallbackVersion527 Version, NifResourceImagePin524? Pin) CaptureCallback529()
    {
        NativeResourceTypeTable527? owner = table;
        owner?.EnterRead527();
        try
        {
            lock (gate)
            {
                NativeResourceCallbackVersion527 version = current;

                return (version, version.RetainCallback529());
            }
        }
        finally
        {
            owner?.LeaveRead527();
        }
    }

    internal void Destroy524(nint payload)
    {
        var selected = CaptureCallback529();
        using var invocation = selected.Pin;
        selected.Version.Image527.InvokeDestructor524(selected.Version.Callbacks527.Destructor, payload);
    }

    internal unsafe bool DynamicCall527(NifNativeCallEnvironment env, nint payload, nint data)
    {
        var selected = CaptureCallback529();
        using var invocation = selected.Pin;
        nint function = selected.Version.Callbacks527.DynamicCall;
        if (function == 0) return false;
        ((delegate* unmanaged[Cdecl]<nint, nint, nint, void>)function)(env.Pointer, payload, data);

        return true;
    }

    public void Dispose()
    {
        NativeResourceCallbackVersion527 release;
        lock (gate)
        {
            if (payloads != 0 || pending != 0) throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            release = current;
            if (handle.IsAllocated)
            {
                NativeResourceTypes527.Forget527(this);
                handle.Free();
            }
        }
        release.Dispose();
    }
}
