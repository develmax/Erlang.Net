namespace Erlang;

internal sealed unsafe partial class NifNativeWindowsCallbacks
{
    private NifNativeBoundMutex795? boundMutex795;

    internal void BindMutexes795(NifNativeAllocationOwner owner)
    {
        if (pointer == 0 || boundMutex795 is not null)
            throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);
        boundMutex795 = new(owner);
        var slots = (nint*)pointer;
        slots[NifNativeMutex795Names.CreateSlot] = boundMutex795.CreatePointer;
        slots[NifNativeMutex795Names.DestroySlot] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeMutex795Callbacks.Destroy;
        slots[NifNativeMutex795Names.TryLockSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, int>)&NifNativeMutex795Callbacks.TryLock;
        slots[NifNativeMutex795Names.LockSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeMutex795Callbacks.Lock;
        slots[NifNativeMutex795Names.UnlockSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeMutex795Callbacks.Unlock;
        slots[NifNativeMutex795Names.NameSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&NifNativeMutex795Callbacks.Name;
    }
}
