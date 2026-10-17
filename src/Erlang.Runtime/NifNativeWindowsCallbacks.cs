using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeWindowsCallbacks : IDisposable
{
    private nint pointer;
    internal nint Pointer => pointer;

    internal NifNativeWindowsCallbacks() : this(false)
    {
    }

    internal NifNativeWindowsCallbacks(bool reallocationEnabled)
    {
        if (IntPtr.Size != NifNativeAbi.PointerBytes)
            throw new PlatformNotSupportedException(NifNativeDiagnostics.CallbackPlatform);
        pointer = (nint)NativeMemory.AllocZeroed((nuint)((NifNativeWindowsCallbackLayout.FunctionCount + NifNativeWindowsCallbackLayout.TailPointers) * IntPtr.Size));
        if (pointer == 0)
            throw new OutOfMemoryException();
        var slots = (nint*)pointer;
        if (reallocationEnabled)
            slots[NifNativeWindowsCallbackLayout.Reallocate] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint>)&NifNativePrimitiveCallbacks.Reallocate;
        slots[NifNativeWindowsCallbackLayout.MakeTupleFromArray] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)&NifNativeArrayCallbacks.MakeTuple;
        slots[NifNativeWindowsCallbackLayout.MakeListFromArray] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)&NifNativeArrayCallbacks.MakeList;
        slots[NifNativeWindowsCallbackLayout.PrivateData] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&NifNativePrimitiveCallbacks.PrivateData;
        slots[NifNativeWindowsCallbackLayout.Allocate] = (nint)(delegate* unmanaged[Cdecl]<nuint, nint>)&NifNativePrimitiveCallbacks.Allocate;
        slots[NifNativeWindowsCallbackLayout.Free] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativePrimitiveCallbacks.Free;
        slots[NifNativeWindowsCallbackLayout.GetInt] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifNativePrimitiveCallbacks.GetInt;
        slots[NifNativeWindowsCallbackLayout.BadArgument] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifNativePrimitiveCallbacks.BadArgument;
        slots[NifNativeWindowsCallbackLayout.MakeInt] = (nint)(delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifNativePrimitiveCallbacks.MakeInt;
        slots[NifNativeWindowsCallbackLayout.IsException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativePrimitiveCallbacks.IsException;
        slots[NifNativeWindowsCallbackLayout.HasPendingException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, int>)&NifNativePrimitiveCallbacks.HasPendingException;
        slots[NifNativeWindowsCallbackLayout.RaiseException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint>)&NifNativePrimitiveCallbacks.RaiseException;
        slots[NifNativeWindowsCallbackLayout.MakeString] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, int, nuint>)&NifNativePrimitiveCallbacks.MakeString;
    }

    public void Dispose()
    {
        nint previous = Interlocked.Exchange(ref pointer, 0);
        NativeMemory.Free((void*)previous);
    }
}
