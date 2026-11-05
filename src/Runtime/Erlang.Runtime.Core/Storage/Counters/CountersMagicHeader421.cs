using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct CountersMagicHeader421
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal nint ReferenceCount;
    internal nuint OriginalSize;
    internal nint Destructor;
    internal uint Reference0;
    internal uint Reference1;
    internal uint Reference2;
    internal int AllocationKind;
    internal CountersMagicData421 Data;
    internal ulong Length { get => Data.Value.Length; set => Data.Value.Length = checked((nuint)value); }
#if DEBUG
    internal ulong CacheLineCount591 { get => Data.Value.CacheLineCount; set => Data.Value.CacheLineCount = checked((nuint)value); }
#endif
}
