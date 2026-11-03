namespace Erlang;

internal sealed class PreparedOwnedMapNode526(PreparedOwnedMap517 owner, int index)
{
    internal PreparedOwnedMap517 Owner { get; } = owner;
    internal int Index { get; } = index;
    internal Term Source => Owner.SourceNodeTerms526[Index]
        ?? throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
    internal OwnedMapNodePlan517 Shape => Owner.Nodes[Index];
    internal int PairCount => Owner.NodePairCounts526[Index];

    internal Term[] Children526() => Source switch
    {
        Cons cons => [cons.Head, cons.Tail],
        TupleTerm tuple => tuple.Items.ToArray(),
        HamtBitmapTerm429 bitmap => bitmap.Children.ToArray(),
        _ => throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape)
    };

    internal OwnedMapHeaderKind517 Kind => Shape.Row >= 0 ? OwnedMapHeaderKind517.HamtLeafCons
        : Shape.Collision ? OwnedMapHeaderKind517.HamtCollision : OwnedMapHeaderKind517.HamtBitmap;
}
