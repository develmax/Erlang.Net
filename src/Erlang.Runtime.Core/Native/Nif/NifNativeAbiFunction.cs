using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeAbiFunction
{
    public nint Name;
    public uint Arity;
    public nint Function;
    public uint Flags;
}
