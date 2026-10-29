using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeAbiFunctionLegacy651
{
    public nint Name;
    public uint Arity;
    public nint Function;
}
