using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct CountersMagicHeader421
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal long ReferenceCount;
    internal nuint OriginalSize;
    internal nint Destructor;
    internal uint Reference0;
    internal uint Reference1;
    internal uint Reference2;
    internal int AllocationKind;
    internal ulong Length;
#if DEBUG
    internal ulong CacheLineCount591;
#endif
}
