namespace Erlang;

internal static partial class NativeMapMaterialization505
{
    internal static MapTerm PublishCopied510(
        ProcessRuntime runtime,
        NativeMapMaterializedLayout505 original,
        KeyValuePair<Term, Term>[] physical,
        HamtViewNode? root,
        KeyValuePair<Term, Term>[] canonical,
        KeyValuePair<Term, Term>[] arithmetic,
        TupleTerm? flatKeys
    )
    {
        if (!original.Matches(runtime))
            throw new NotSupportedException(NativeMapCopy510Names.ForeignDomain);
        var layout = new NativeMapMaterializedLayout505(
            runtime,
            original.Domain,
            physical,
            root,
            original.HashRetention
        );
        var map = MapTerm.FromCopiedNativeLayout510(
            physical,
            canonical,
            arithmetic,
            flatKeys,
            root is null ? null : layout.Lookup,
            layout.EqualityPlan
        );
        layouts.Add(map, layout);

        return map;
    }
}
