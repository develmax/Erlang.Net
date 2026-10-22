namespace Erlang;

internal sealed class ReCode411(
    IntPtr pointer,
    uint captures,
    byte unicode,
    bool crlf
)
{
    internal IntPtr Pointer = pointer;
    internal uint Captures { get; } = captures;
    internal byte UnicodeMetadata { get; } = unicode;
    internal bool Unicode => UnicodeMetadata != 0;
    internal bool Crlf { get; } = crlf;
    internal int Readers;
    internal bool GuestReleased;
}
