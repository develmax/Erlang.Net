using System.Buffers.Binary;

namespace Erlang;

internal static class ReSerializedLayout413
{
    private const int SerializedHeaderBytes = 16;
    private const int CodeCountOffset = 12;
    private const int VersionOffset = 4;
    private const int ConfigurationOffset = 8;
    private const uint Magic = 0x50523253;
    private const uint Version = 10 | (47 << 16);
    private const uint Configuration = 1 | (8 << 8) | (8 << 16);
    private const int CharacterTablesBytes = 1088;
    private const int CodeHeaderBytes = 152;
    private const int BlockSizeOffset = 72;
    private const int CodeStartOffset = 80;
    private const int CodeMagicOffset = 88;
    private const uint CodeMagic = 0x50435245;
    private const int NameSizeOffset = 140;
    private const int NameCountOffset = 142;
    private const int MaximumNameEntryBytes = 128 + 2 + 1;
    private const int MaximumNameCount = 10000;

    // The native decoder has no buffer-length parameter. Authenticate the complete
    // first code extent before permitting it to read from an owned pinned buffer.
    internal static bool RequiresFallback(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < SerializedHeaderBytes)
            throw new NotSupportedException(ReExtendedNames413.UnsupportedSerializedPattern);
        if (BinaryPrimitives.ReadInt32LittleEndian(bytes[CodeCountOffset..]) <= 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes[VersionOffset..]) != Version
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes[ConfigurationOffset..]) != Configuration)
            return true;
        int offset = SerializedHeaderBytes + CharacterTablesBytes;
        if (bytes.Length - offset < CodeHeaderBytes)
            throw new NotSupportedException(ReExtendedNames413.UnsupportedSerializedPattern);
        var code = bytes[offset..];
        ulong length = BinaryPrimitives.ReadUInt64LittleEndian(code[BlockSizeOffset..]);
        if (length <= CodeHeaderBytes)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (length > (ulong)code.Length)
            throw new NotSupportedException(ReExtendedNames413.UnsupportedSerializedPattern);
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(code[CodeMagicOffset..]);
        int nameSize = BinaryPrimitives.ReadUInt16LittleEndian(code[NameSizeOffset..]);
        int nameCount = BinaryPrimitives.ReadUInt16LittleEndian(code[NameCountOffset..]);
        if (magic != CodeMagic || nameSize > MaximumNameEntryBytes || nameCount > MaximumNameCount)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ulong start = BinaryPrimitives.ReadUInt64LittleEndian(code[CodeStartOffset..]);
        if (start < CodeHeaderBytes || start >= length
            || (ulong)(CodeHeaderBytes + nameSize * nameCount) > length)
            throw new NotSupportedException(ReExtendedNames413.UnsupportedSerializedPattern);

        return false;
    }
}
