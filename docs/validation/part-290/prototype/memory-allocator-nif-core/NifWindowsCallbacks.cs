using System.Runtime.InteropServices;

namespace NifPrototype;

internal sealed unsafe class NifWindowsCallbacks : IDisposable
{
    private nint pointer;
    internal nint Pointer => pointer;

    internal NifWindowsCallbacks()
    {
        if (IntPtr.Size != 8)
            throw new PlatformNotSupportedException("Pinned fixture callback table requires Windows x64.");
        pointer = (nint)NativeMemory.AllocZeroed((nuint)((NifWindowsCallbackLayout.FunctionCount + NifWindowsCallbackLayout.TailPointers) * IntPtr.Size));
        if (pointer == 0)
            throw new OutOfMemoryException();
        var slots = (nint*)pointer;
        slots[NifWindowsCallbackLayout.PrivateData] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&NifPrimitiveCallbacks.PrivateData;
        slots[NifWindowsCallbackLayout.Allocate] = (nint)(delegate* unmanaged[Cdecl]<nuint, nint>)&NifPrimitiveCallbacks.Allocate;
        slots[NifWindowsCallbackLayout.Free] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifPrimitiveCallbacks.Free;
        slots[NifWindowsCallbackLayout.GetInt] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifPrimitiveCallbacks.GetInt;
        slots[NifWindowsCallbackLayout.BadArgument] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifPrimitiveCallbacks.BadArgument;
        slots[NifWindowsCallbackLayout.MakeInt] = (nint)(delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifPrimitiveCallbacks.MakeInt;
        slots[NifWindowsCallbackLayout.MakeString] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, int, nuint>)&NifPrimitiveCallbacks.MakeString;
    }

    public void Dispose()
    {
        nint previous = Interlocked.Exchange(ref pointer, 0);
        NativeMemory.Free((void*)previous);
    }
}
