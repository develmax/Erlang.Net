namespace Erlang;

internal static class ResourceBinary577Names
{
    internal const int Words = 5;
    internal const int HeaderWords = 1;
    internal const int DataWords = 4;
    internal const int BaseWord = 1;
    internal const int StartWord = 2;
    internal const int EndWord = 3;
    internal const int OriginWord = 4;
    internal const nuint Header = (4u << 6) | (9u << 2);
    internal const nuint FlagMask = 3;
    internal const nuint BoxedTag = 2;
    internal const string Invalid = "The resource binary SubBits/BinRef header is invalid, foreign, stale, or outside its admitted native range.";
    internal const string Custody = "A resource binary header requires the actual rooted construction input.";
}
