using System.Runtime.InteropServices;

namespace Erlang;

// Pinned native-word Binary prefix. Bytes immediately follow this physical prefix.
[StructLayout(LayoutKind.Sequential)]
internal struct OrdinaryBinaryHeader
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal nint References;
    internal nint OriginalSize;
}
