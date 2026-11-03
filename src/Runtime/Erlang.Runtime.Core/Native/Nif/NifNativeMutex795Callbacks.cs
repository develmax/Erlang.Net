using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMutex795Callbacks
{
    // Linux bridge proposal only: scoped native calls have a real allocation
    // owner. Unscoped worker creation requires a bound per-image native thunk.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Create(byte* name) => CreateForOwner(NifNativeCallbackScope.TryCurrent528?.Allocations, name);

    internal static nint CreateForOwner(NifNativeAllocationOwner? owner, byte* name)
    {
        try
        {
            if (owner is null) throw new InvalidOperationException(NifNativeDiagnostics.NativeThreadUnsupported);

            return owner.CreateMutex795(name);
        }
        catch (OutOfMemoryException)
        {
            return 0; // Genuine allocation failure; no synthetic OOM path.
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Destroy(nint pointer)
    {
        try
        {
            NifNativeAllocationOwner.DestroyMutex795(pointer);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int TryLock(nint pointer)
    {
        try
        {
            return NifNativeAllocationOwner.ResolveMutex795(pointer).TryLock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMutex795Names.Invalid;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Lock(nint pointer)
    {
        try
        {
            NifNativeAllocationOwner.ResolveMutex795(pointer).Lock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Unlock(nint pointer)
    {
        try
        {
            NifNativeAllocationOwner.ResolveMutex795(pointer).Unlock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Name(nint pointer)
    {
        if (pointer == 0) return 0; // Pinned erl_drv_mutex_name(NULL).
        try
        {
            return NifNativeAllocationOwner.ResolveMutex795(pointer).Name;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }
}
