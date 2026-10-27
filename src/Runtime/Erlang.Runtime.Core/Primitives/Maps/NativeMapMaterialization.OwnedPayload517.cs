namespace Erlang;

internal static partial class NativeMapMaterialization505
{
    internal static MapTerm PublishOwned517(
        ProcessRuntime runtime,
        object domain,
        OwnedMapPayload517 payload,
        TupleTerm? flatKeys,
        HamtViewNode? root,
        NifNativeMapHashRetention504? retention,
        bool logicalOrigin
    )
    {
        var layout = new NativeMapMaterializedLayout505(
            runtime,
            domain,
            payload,
            root,
            retention
        );
        Func<Term, (bool Found, Term? Value)> lookup = root is null ? new OwnedFlatMapLookup517(payload.Physical).Find : layout.Lookup;
        var map = MapTerm.FromOwnedPayload517(
            payload,
            flatKeys,
            lookup,
            layout.EqualityPlan,
            logicalOrigin
        );
        layouts.Add(map, layout);

        return map;
    }
}
