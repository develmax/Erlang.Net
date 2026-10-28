namespace Erlang;

// Called during heap validation/marking. Only immutable graph/CWT reads;
// never requests the map owner, identity provider, lookup or comparison plan.
internal static class NativeMapOwnedRoots510
{
    internal static Term[] Children(Term value)
    {
        if (value is not MapTerm map)
            return new DebugDirtyCopyFrame424(value).Children;
        if (!NativeMapMaterialization505.TryRead(map, null, out var layout))
        {
            Term[] children = new DebugDirtyCopyFrame424(value).Children;

            return map.FlatKeys426 is { } flatKeys ? [.. children, flatKeys] : children;
        }
        var roots = new List<Term>();
        foreach (var pair in map.Entries)
        {
            roots.Add(pair.Key);
            roots.Add(pair.Value);
        }
        foreach (var pair in map.ArithmeticKeyEntries)
        {
            roots.Add(pair.Key);
            roots.Add(pair.Value);
        }
        foreach (var pair in layout.PhysicalEntries)
        {
            roots.Add(pair.Key);
            roots.Add(pair.Value);
        }
        if (map.FlatKeys426 is { } keys)
            roots.Add(keys);
        var nodes = new Stack<HamtViewNode>();
        if (layout.Root is { } root)
            nodes.Push(root);
        while (nodes.TryPop(out var node))
        {
            if (node.Leaf is { } pair)
            {
                roots.Add(pair.Key);
                roots.Add(pair.Value);
            }
            if (node.StoredTerm is { } stored)
                roots.Add(stored);
            foreach (var child in node.Children)
                nodes.Push(child);
        }

        return roots.ToArray();
    }
}
