using System.Runtime.InteropServices;

namespace Erlang.NifFixtureHost;

[StructLayout(LayoutKind.Sequential)]
internal struct NifAbiFunction
{
    public nint Name;
    public uint Arity;
    public nint Function;
    public uint Flags;
}
