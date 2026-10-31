namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal PreparedMapGraph517 PrepareMapGraph517(Term source, bool preserveLocal)
        => PreparedMapGraph517.Capture(
            this,
            runtime,
            source,
            preserveLocal
        );

    internal bool IsLocalMapForPreparation517(MapTerm map)
    {
        lock (Gate)
            return IsOwnedLocal(map);
    }

    internal static IEnumerable<Term> DirectConstructionChildren517(Term value) => value switch
    {
        MapTerm { OwnedMapPayload517: { } payload } => payload.RootPayload,
        HamtBitmapTerm429 bitmap => bitmap.Children,
        Cons cons => new[] { cons.Head, cons.Tail },
        TupleTerm tuple => tuple.Items,
        FunctionTerm function => function.Environment,
        NativeRecordTerm { OwnedRecordStorage: { } storage } record => new Term[] { storage.ReadRecordDefinition() }.Concat(record.Values),
        NativeRecordTerm record => record.Values,
        _ => Array.Empty<Term>()
    };
}
