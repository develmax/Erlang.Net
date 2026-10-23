using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct PrimFileTime410
{
    internal uint Low;
    internal uint High;
    internal readonly ulong Bits => ((ulong)High << 32) | Low;
}
