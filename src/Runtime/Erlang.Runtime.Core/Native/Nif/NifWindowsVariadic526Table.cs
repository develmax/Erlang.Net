using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NifWindowsVariadic526Table
{
    internal uint Version;
    internal uint Size;
    internal uint MaximumTuple;
    internal uint MaximumList;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint> Tuple;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint> List;
    internal delegate* unmanaged[Cdecl]<nint, nuint> SystemLimit;
    internal delegate* unmanaged[Cdecl]<nuint, long, nint, nint, int> FormatTerm791;
}
