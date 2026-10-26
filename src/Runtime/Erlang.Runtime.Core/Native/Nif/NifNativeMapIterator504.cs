using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct NifNativeMapIterator504
{
    internal nuint Map;
    internal nuint Size;
    internal nuint Index;
    internal nuint Position0;
    internal nuint Position1;
    internal nuint State;
    internal nuint Spare;
}
