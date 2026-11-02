using System.Runtime.InteropServices;

namespace Erlang;

// Actual managed VM entry ABI. Fun.entry points here and dispatch consumes it.
[StructLayout(LayoutKind.Sequential)]
internal struct ManagedFunctionDispatchHeader
{
    internal nint Target;
    internal ulong Signature;
}
