using System.Runtime.InteropServices;

namespace Erlang;

// Each actual image callback table owns this delegate. Creation from native
// workers has no env argument and therefore cannot infer ownership from TLS.
internal sealed unsafe class NifNativeBoundMutex795
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CreateCallback(byte* name);
    private readonly CreateCallback create;
    internal nint CreatePointer { get; }

    internal NifNativeBoundMutex795(NifNativeAllocationOwner owner)
    {
        create = name => NifNativeMutex795Callbacks.CreateForOwner(owner, name);
        CreatePointer = Marshal.GetFunctionPointerForDelegate(create);
    }
}
