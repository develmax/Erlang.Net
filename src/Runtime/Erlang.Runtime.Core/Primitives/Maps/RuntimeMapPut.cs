namespace Erlang;

internal static class RuntimeMapPut
{
    internal static MapTerm Put(
        ProcessRuntime runtime,
        MapTerm source,
        Term key,
        Term value
    )
    {
        if (source.FlatKeys426 is not null && source.NativeEquality505 is null
            && !NativeMapMaterialization505.TryRead(source, runtime, out _)
            && (source.Entries.Count < HamtViewLayout.ReleaseFlatMapLimit || source.TryGet(key, out _)))
            return source.SourcePut(key, value);

        // Reuse the immutable native map graph and its hash retention. A HAMT
        // collision update uses CMP_TERM, whereas a flat update searches EQ.
        NifNativeMapGraph504.TryNode(new(source), runtime, out var node);
        var changed = NifNativeMapGraph504.Change(
            node,
            new(key),
            new(value),
            NifNativeMapMutation504Kind.Put,
            runtime,
            out _
        );

        var raw = (MapTerm)NifNativeTermGraph.Resolve(new(changed));
        if (!source.IsOrdinaryLogicalMap517)
            return raw;
        var logical = new MapTerm(raw.ArithmeticKeyEntries);

        return logical.Entries.Count == raw.Entries.Count
            ? NativeMapMaterialization505.PublishLogicalEscape517(raw, logical) : raw;
    }
}
