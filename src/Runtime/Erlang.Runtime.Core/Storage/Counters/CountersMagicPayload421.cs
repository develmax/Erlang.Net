using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct CountersMagicPayload421
{
    internal nuint Length;
#if DEBUG
    internal nuint CacheLineCount;
#endif
}
