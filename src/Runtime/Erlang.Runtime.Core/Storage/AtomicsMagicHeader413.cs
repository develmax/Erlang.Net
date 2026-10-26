using System.Runtime.InteropServices;
namespace Erlang;
// Authoritative owned orig_bytes region: magic identity prefix24 + AtomicsRef fields16.
// The real native allocation begins here; no unmanaged Binary internals or CLR heap estimate is reported.
[StructLayout(LayoutKind.Sequential)]
internal struct AtomicsMagicHeader413
{
    internal nint Destructor;
    internal uint Reference0;
    internal uint Reference1;
    internal uint Reference2;
    internal int AllocationKind;
    internal int IsSigned;
    internal ulong Length;
}
