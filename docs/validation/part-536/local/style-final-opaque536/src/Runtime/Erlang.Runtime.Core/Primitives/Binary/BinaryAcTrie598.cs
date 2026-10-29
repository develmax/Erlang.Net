using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BinaryAcTrie598
{
    internal uint Counter;
    internal nint Root;
    internal nuint RootByteCount;
    internal fixed byte RootBytes[BinaryArenaNames598.AcRootSimdMax];
}
