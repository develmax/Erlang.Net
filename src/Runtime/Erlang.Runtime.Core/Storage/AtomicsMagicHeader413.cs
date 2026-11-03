using System.Runtime.InteropServices;

namespace Erlang;

// Source-derived ARCH64 complete ErtsMagicBinary prefix followed by AtomicsRef.
// This represents the ACTUAL allocation; info(memory) reads OriginalSize below.
[StructLayout(LayoutKind.Sequential)]
internal struct AtomicsMagicHeader413
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
    internal int IsSigned;
    internal ulong Length;
}
