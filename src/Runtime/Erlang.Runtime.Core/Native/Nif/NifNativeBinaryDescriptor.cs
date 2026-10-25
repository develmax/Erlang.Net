using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NifNativeBinaryDescriptor
{
    internal nuint Size;
    internal byte* Data;
    internal nint Reference;
    internal nint Spare0;
    internal nint Spare1;
}
