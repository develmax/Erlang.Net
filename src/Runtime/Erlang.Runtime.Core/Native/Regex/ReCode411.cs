namespace Erlang;

internal sealed class ReCode411
{
    internal ReMagicBacking597 Backing597 { get; }
    internal nint Pointer => Backing597.CodePointer597;
    internal uint Captures { get; }
    internal byte UnicodeMetadata { get; }
    internal bool Unicode => UnicodeMetadata != 0;
    internal bool Crlf { get; }
    internal int Readers;
    internal bool GuestReleased;

    internal ReCode411(
        ReMagicOwner597 owner,
        nint pointer,
        uint captures,
        byte unicode,
        bool crlf
    )
    {
        Captures = captures;
        UnicodeMetadata = unicode;
        Crlf = crlf;
        Backing597 = owner.Create597(this, pointer);
    }
}
