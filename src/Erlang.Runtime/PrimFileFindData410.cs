using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct PrimFileFindData410
{
    internal uint Attributes;
    internal PrimFileTime410 Creation;
    internal PrimFileTime410 Access;
    internal PrimFileTime410 Write;
    internal uint SizeHigh;
    internal uint SizeLow;
    internal uint Reserved0;
    internal uint Reserved1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    internal string Name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    internal string Alternate;
}
