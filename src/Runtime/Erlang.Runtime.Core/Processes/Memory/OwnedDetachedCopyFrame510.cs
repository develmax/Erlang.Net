namespace Erlang;

internal sealed class OwnedDetachedCopyFrame510
{
    internal Term Source { get; }
    internal Term[] Children { get; }
    internal Term[] Copied { get; }
    internal int Index { get; set; }
    internal NativeMapCopyPlan510? NativeMap { get; }

    internal OwnedDetachedCopyFrame510(ProcessRuntime runtime, Term source)
    {
        Source = source;
        bool hasLayout = source is MapTerm candidate && NativeMapMaterialization505.TryRead(candidate, null, out _);
        if (source is MapTerm map && (map.NativeEquality505 is not null || hasLayout)
            && (!map.IsOrdinaryLogicalMap517 || !NativeMapMaterialization505.TryRead(map, null, out var saved) || saved.Matches(runtime)))
        {
            NativeMap = new(runtime, map);
            Children = NativeMap.Children;
        }
        else
            Children = new DebugDirtyCopyFrame424(source).Children;
        Copied = new Term[Children.Length];
    }
}
