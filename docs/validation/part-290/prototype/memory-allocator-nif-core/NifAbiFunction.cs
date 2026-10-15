using System.Runtime.InteropServices;

namespace NifPrototype;

[StructLayout(LayoutKind.Sequential)]
internal struct NifAbiFunction
{
    public nint Name;
    public uint Arity;
    public nint Function;
    public uint Flags;
}
