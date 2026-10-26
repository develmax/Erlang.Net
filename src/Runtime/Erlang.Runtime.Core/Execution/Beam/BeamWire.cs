namespace Erlang;

internal static class BeamWire
{
    public const string SourceCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    public const int FormatVersion = 0;
    public const int MaximumArity = 255;
    public const int MaximumAtomCharacters = 255;
    public const int TableAllocationSanityBytes = 1 << 30;
    public const int WordBytes = 8;
    public const int AtomEntryBytes = 8;
    public const int ImportEntryBytes = 24;
    public const int ExportEntryBytes = 16;
    public const int SmallIntegerValueBits = 60;
    public const int CompactSmallPayloadShift = 4;
    public const int CompactLargeSizeShift = 5;
    public const int CompactMediumHighShift = 5;
    public const int CompactMediumWordShift = 8;
    public const int TagMask = 7;
    public const int IntegerTag = 1;
    public const int UnsignedTag = 0;
    public const int LongFlag = 8;
    public const int LargeFlag = 16;
    public const int ExtendedSizeCode = 7;
    public const int ShortLargeBase = 2;
    public const int ExtendedLargeBase = 9;
    public const string AtomsChunk = "AtU8";
    public const string ObsoleteAtomsChunk = "Atom";
    public const string ImportsChunk = "ImpT";
    public const string ExportsChunk = "ExpT";
    public const string CodeChunk = "Code";
}
