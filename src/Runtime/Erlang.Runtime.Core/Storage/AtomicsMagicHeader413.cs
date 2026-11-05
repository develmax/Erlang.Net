using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct AtomicsMagicHeader413
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
    internal AtomicsMagicData413 Data;
    internal int IsSigned { get => Data.Value.IsSigned; set => Data.Value.IsSigned = value; }
    internal ulong Length { get => Data.Value.Length; set => Data.Value.Length = checked((nuint)value); }
}
