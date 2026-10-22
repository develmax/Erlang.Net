using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct CountersMagicHeader421
{
    internal nint Destructor;
    internal uint Reference0;
    internal uint Reference1;
    internal uint Reference2;
    internal int AllocationKind;
    internal ulong Length;
}
