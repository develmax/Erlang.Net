using System.Runtime.InteropServices;

namespace Erlang;

// Pinned ARCH_64 Binary prefix. Bytes immediately follow this physical prefix.
[StructLayout(LayoutKind.Sequential)]
internal struct OrdinaryBinaryHeader
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal long References;
    internal nint OriginalSize;
}
