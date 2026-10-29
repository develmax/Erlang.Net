using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BinaryAcNode598
{
    internal uint Depth;
    internal int Final;
    internal nint Failure;
    internal fixed ulong Transitions[BinaryArenaNames598.Alphabet];
}
