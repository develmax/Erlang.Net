namespace Erlang;

internal static partial class NativeMapMaterialization505
{
    internal static MapTerm BuildSourceBulk(
        ProcessRuntime runtime,
        IReadOnlyList<KeyValuePair<Term, Term>> physical,
        HamtViewNode root,
        HamtHashLeases429 ownership
    )
    {
        var retention = new NifNativeMapHashRetention504(ownership);
        var layout = new NativeMapMaterializedLayout505(
            runtime,
            runtime.NativeMapDomain505(),
            physical,
            root,
            retention
        );
        MapTerm raw = MapTerm.FromNativeLayout504(physical, layout.Lookup, layout.EqualityPlan);
        layouts.Add(raw, layout);
        // Keep the established ordinary-host facade only when it can represent
        // every actual occurrence. A physical duplicate must never disappear.
        var logical = new MapTerm(physical);
        if (logical.Entries.Count == physical.Count)
            return PublishLogicalEscape517(raw, logical);

        return raw;
    }
}
