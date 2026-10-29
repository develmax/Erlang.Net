using System.Runtime.InteropServices;

namespace Erlang;

// Exact supported ARCH64 ErtsMagicBinary prefix, then regex_magic_indirect.
[StructLayout(LayoutKind.Sequential)]
internal struct ReMagicHeader597
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal long RefCount;
    internal nuint OriginalSize;
    internal nint Destructor;
    internal uint Ref0;
    internal uint Ref1;
    internal uint Ref2;
    internal int AllocationKind;
    internal nint Code;
}
