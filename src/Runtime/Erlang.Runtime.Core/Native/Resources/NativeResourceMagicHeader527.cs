using System.Runtime.InteropServices;

namespace Erlang;

// Physical x64 release-build prefix, from pinned erl_binary.h:54-103.
// RefCount and Ref0..2 describe this actual allocation and VM identity.
[StructLayout(LayoutKind.Sequential)]
internal struct NativeResourceMagicHeader527
{
    internal nuint Flags;
    internal nuint ApparentSize;
    internal long RefCount;
    internal nuint OriginalSize;
    internal nint Destructor;
    internal uint Ref0;
    internal uint Ref1;
    internal uint Ref2;
    internal int Allocator;
}
