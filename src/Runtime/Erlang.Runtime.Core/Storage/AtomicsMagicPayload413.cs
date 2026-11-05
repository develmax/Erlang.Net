using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct AtomicsMagicPayload413
{
    internal int IsSigned;
    internal nuint Length;
}
