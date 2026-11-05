using System.Runtime.InteropServices;

namespace Erlang;

// CLR scratch representation of erl_map.c hxnode_t on the admitted 64-bit ABI.
// PairIndex identifies the immutable input pair instead of retaining a native
// Eterm pointer. All four source word slots remain present for pivot offsets.
[StructLayout(LayoutKind.Sequential)]
internal struct RuntimeMapSourceNode
{
    internal ulong Hash;
    internal ulong Skip;
    internal ulong Ordinal;
    internal ulong PairIndex;
}
