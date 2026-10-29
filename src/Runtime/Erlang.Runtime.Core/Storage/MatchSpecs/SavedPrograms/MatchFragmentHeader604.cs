using System.Runtime.InteropServices;

namespace Erlang;

// x64: erl_message.h ErlHeapFragment, with the flexible mem[] omitted.
[StructLayout(LayoutKind.Sequential)]
internal struct MatchFragmentHeader604
{
    internal nint Next;
    internal nint OffHeapFirst;
    internal nuint OffHeapOverhead;
    internal nuint AllocatedWords;
    internal nuint UsedWords;
}
