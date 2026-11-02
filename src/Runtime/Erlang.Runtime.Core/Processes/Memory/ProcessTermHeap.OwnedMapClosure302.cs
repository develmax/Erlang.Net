namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private bool IsImmutableMapLiteralCell302(Term value) => IsImmutableMapCell302(value, MapClosure302.Literal);

    private bool IsImmutableMapReadonlyCell302(Term value) => IsImmutableMapCell302(value, MapClosure302.Readonly);

    private bool IsImmutableMapNoBackingCell302(Term value) => IsImmutableMapCell302(value, MapClosure302.NoBacking);

    private bool IsImmutableMapNoBinaryCell302(Term value) => IsImmutableMapCell302(value, MapClosure302.NoBinary);

    private bool IsImmutableMapCell302(Term value, MapClosure302 kind)
    {
        // This is an existing exact loader provenance seam, never a generic
        // portable Tuple shape. Validate EVERY actual immutable key too.
        if (value is TupleTerm keys && IsLoaderMapKeys565(keys))
        {
            foreach (Term key in keys.Items)
                if (!Child(key)) return false;

            return true;
        }

        return Child(value);

        bool Child(Term child) => kind switch
        {
            MapClosure302.Literal => IsImmutableLiteralOnlyClosure132(child),
            MapClosure302.Readonly => IsImmutableReadonlyCaptureClosure138(child),
            MapClosure302.NoBacking => IsImmutableNoBackingClosure192(child),
            MapClosure302.NoBinary => IsImmutableNoBinaryClosure192(child),
            _ => false
        };
    }

    internal bool CanUseOwnedMapClosure302(Term value, ProcessTermSequence506 block)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!ReferenceEquals(block.Owner, this) || !ReferenceEquals(Storage(value), block)) return false;
        _ = IsOwnedLocal(value);
        if (!mapHeaders517.ContainsKey(block.Identity)) return false;
        OwnedMapHeader517 header = ReadLiteralMapHeaderCore800(block);
        if (header.PayloadArity != block.Count || block.RawPayloadWords556 != 0 || block.IsMutableSequenceToken814) return false;
        if (value is MapTerm map)
        {
            // Exact private immutable payload/table materialization, not an
            // arbitrary pending NIF map or a host facade with equal entries.
            if (map.OwnedMapPayload517 is not OwnedMapPayload517 payload
                || !ReferenceEquals(payload.RootPayload, block)
                || !ReferenceEquals(map.Entries, payload.Canonical)
                || !ReferenceEquals(map.ArithmeticKeyEntries, payload.Arithmetic)
                || !NativeMapMaterialization505.TryRead(map, runtime, out NativeMapMaterializedLayout505 layout)
                || !ReferenceEquals(layout.PhysicalEntries, payload.Physical)) return false;

            return header.Kind switch
            {
                OwnedMapHeaderKind517.FlatMap => layout.Root is null && map.FlatKeys426 is not null
                    && block.Count == header.PairCount + 1 && ReferenceEquals(block[0], map.FlatKeys426),
                OwnedMapHeaderKind517.HamtHead => map.FlatKeys426 is null && layout.Root is not null
                    && layout.Root.MatchesOwnedPayload302(this, block),
                _ => false
            };
        }
        if (value is HamtBitmapTerm429 bitmap)
            return header.Kind == OwnedMapHeaderKind517.HamtBitmap && ReferenceEquals(bitmap.OwnedStorage517, block)
                && header.Bitmap == bitmap.Bitmap && block.Count == System.Numerics.BitOperations.PopCount((uint)bitmap.Bitmap);

        return value switch
        {
            Cons => header.Kind == OwnedMapHeaderKind517.HamtLeafCons && block.Count == 2,
            TupleTerm => header.Kind == OwnedMapHeaderKind517.HamtCollision,
            _ => false
        };
    }

    private enum MapClosure302
    {
        Literal,
        Readonly,
        NoBacking,
        NoBinary
    }
}
