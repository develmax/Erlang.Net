using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NifNativeBridgeTable
{
    internal uint Version;
    internal uint Size;
    internal delegate* unmanaged[Cdecl]<nint, nint> PrivateData;
    internal delegate* unmanaged[Cdecl]<nuint, nint> Allocate;
    internal delegate* unmanaged[Cdecl]<nint, void> Free;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int*, int> GetInt;
    internal delegate* unmanaged[Cdecl]<nint, nuint> BadArgument;
    internal delegate* unmanaged[Cdecl]<nint, int, nuint> MakeInt;
    internal delegate* unmanaged[Cdecl]<nint, byte*, int, nuint> MakeString;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsException;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, int> HasPendingException;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nuint> RaiseException;
    internal delegate* unmanaged[Cdecl]<nint, uint, nint, nuint> MakeTuple;
    internal delegate* unmanaged[Cdecl]<nint, uint, nint, nuint> MakeList;
}
