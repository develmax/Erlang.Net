using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct PrimFileInformation410
{
    internal uint Attributes;
    internal PrimFileTime410 Creation;
    internal PrimFileTime410 Access;
    internal PrimFileTime410 Write;
    internal uint Volume;
    internal uint SizeHigh;
    internal uint SizeLow;
    internal uint Links;
    internal uint IndexHigh;
    internal uint IndexLow;
    internal readonly ulong Size => ((ulong)SizeHigh << 32) | SizeLow;
}
