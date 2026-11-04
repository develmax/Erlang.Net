namespace Erlang;

internal static class EtsDisplayLayout
{
    internal const int DefaultHeight = 40;
    internal const int DefaultWidth = 80;
    internal const int ObjectPrefixWidth = 8;
    internal const int PrintCommandCodePoint = 112;
    internal const int RegexCommandCodePoint = 47;
    internal const int SpaceCodePoint = 32;
    internal const int TruncatedObjectOverhead = 13;

    private const int IdentifierColumnWidth = 15;
    private const int NameColumnWidth = 17;
    private const int TypeColumnWidth = 5;
    private const int SizeColumnWidth = 6;
    private const int MemoryColumnWidth = 8;
    private const int OwnerColumnWidth = 0;

    internal static readonly int[] ColumnWidths =
    [
        IdentifierColumnWidth,
        NameColumnWidth,
        TypeColumnWidth,
        SizeColumnWidth,
        MemoryColumnWidth,
        OwnerColumnWidth
    ];
}
