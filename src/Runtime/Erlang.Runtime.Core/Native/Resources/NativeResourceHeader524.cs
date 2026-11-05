using System.Runtime.InteropServices;

namespace Erlang;

// Field view; release ARCH_32 tail padding is included by NativeResourceMagic527.UserOffset527.
[StructLayout(LayoutKind.Sequential)]
internal struct NativeResourceHeader524
{
    internal nint Type;
    internal nint Monitors;
}
