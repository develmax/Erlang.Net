namespace Erlang;

internal static class NativeOffheap576Names
{
    // Pinned ARCH_64 known-endian ErtsMRefThing and BinRef share these offsets.
    internal const int Words = 3;
    internal const int HeaderWords = 1;
    internal const int DataWords = 2;
    internal const int BinaryWord = 1;
    internal const int NextWord = 2;
    internal const nuint BinaryHeader = 0xA8;

    internal static nuint Header(NativeOffheapKind576 kind) => kind switch
    {
        NativeOffheapKind576.MagicReference => OrdinaryReference562Names.Header,
        NativeOffheapKind576.BinaryReference => BinaryHeader,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
