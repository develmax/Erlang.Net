namespace Erlang;

// OTP29.1.1 erl_term.h _HEADER_ARITY_OFFS=6 and MAP_HEADER_* widths 2/8/16.
// These numeric bits occupy real native words immediately before payload cells.
internal static class EncodedMapHeader526
{
    internal const int ArityShift = 6;
    internal const int MapTypeBits = 2;
    internal const int MapArityBits = 8;
    internal const nuint MapTag = 0x2c;
    internal const nuint FlatType = 0;
    internal const nuint BitmapType = 1;
    internal const nuint ArrayHeadType = 2;
    internal const nuint BitmapHeadType = 3;
    internal const ushort FullBitmap = ushort.MaxValue;

    internal static nuint Tuple(int arity) => checked((nuint)arity << ArityShift);

    private static nuint Map(nuint type, nuint arity, ushort bitmap)
        => ((((nuint)bitmap << MapArityBits | arity) << MapTypeBits | type) << ArityShift) | MapTag;

    internal static int WordCount(OwnedMapHeader517 header) => header.Kind switch
    {
        OwnedMapHeaderKind517.HamtLeafCons => 0,
        OwnedMapHeaderKind517.FlatMap or OwnedMapHeaderKind517.HamtHead => 2,
        _ => 1
    };

    internal static nuint First(OwnedMapHeader517 header) => header.Kind switch
    {
        OwnedMapHeaderKind517.FlatKeysTuple or OwnedMapHeaderKind517.HamtCollision => Tuple(header.PayloadArity),
        OwnedMapHeaderKind517.FlatMap => Map(FlatType, 1, 0),
        OwnedMapHeaderKind517.HamtHead => Map(header.Bitmap == FullBitmap ? ArrayHeadType : BitmapHeadType, 1, header.Bitmap),
        OwnedMapHeaderKind517.HamtBitmap => Map(BitmapType, 0, header.Bitmap),
        _ => throw new InvalidOperationException(CellStorage526Names.InvalidHeader)
    };

    internal static nuint[] Words(OwnedMapHeader517 header) => WordCount(header) switch
    {
        0 => [],
        1 => [First(header)],
        2 => [First(header), checked((nuint)header.PairCount)],
        _ => throw new InvalidOperationException(CellStorage526Names.InvalidHeader)
    };
}
