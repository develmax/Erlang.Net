namespace Erlang;

internal static class MagicReference576Names
{
    // ARCH_64 and known little endianness share the ordinary reference header.
    // The overlaid ordinary marker distinguishes mb from inline ref numbers.
    internal static nuint Header => OrdinaryReference562Names.Header;
    internal const int Words = 3;
    internal const int HeaderWords = 1;
    internal const int DataWords = 2;
    internal const int BinaryWord = 1;
    internal const int NextWord = 2;
    internal const string Invalid = "The registered magic-reference header or offheap chain is invalid.";
    internal const string MissingCustody = "Magic-reference construction requires the actual rooted resource input.";
    internal const string Unpublished = "An uncommitted magic-reference construction cannot escape its owning transaction.";
}
