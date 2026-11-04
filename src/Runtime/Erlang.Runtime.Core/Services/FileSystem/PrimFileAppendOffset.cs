using System.Runtime.InteropServices;

namespace Erlang;

// OVERLAPPED layout from WinBase.h; both offset words select atomic EOF writes.
[StructLayout(LayoutKind.Sequential)]
internal struct PrimFileAppendOffset
{
    internal nuint Internal;
    internal nuint InternalHigh;
    internal uint Offset;
    internal uint OffsetHigh;
    internal nint Event;
}
