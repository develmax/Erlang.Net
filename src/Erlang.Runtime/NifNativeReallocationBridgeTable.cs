using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NifNativeReallocationBridgeTable
{
    internal uint Version;
    internal uint Size;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nint> Reallocate;
}
