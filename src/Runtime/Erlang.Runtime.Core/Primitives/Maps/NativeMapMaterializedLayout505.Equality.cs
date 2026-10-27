namespace Erlang;

internal sealed partial class NativeMapMaterializedLayout505
{
    internal NativeMapEqualityPlan505? EqualityPlan(MapTerm other)
    {
        if (PhysicalEntries.Count != other.Entries.Count)
            return new(false, []);
        var comparisons = new List<KeyValuePair<Term, Term>>();
        if (Root is null)
        {
            var peer = NativeMapFlatEntries505.Read(other);
            for (int index = 0; index < PhysicalEntries.Count; index++)
                comparisons.Add(new(PhysicalEntries[index].Key, peer[index].Key));
            for (int index = 0; index < PhysicalEntries.Count; index++)
                comparisons.Add(new(PhysicalEntries[index].Value, peer[index].Value));

            return new(true, comparisons);
        }
        var runtime = RequireOwner();
        using var read = runtime.AcquireHamtIndex429(other);
        var pending = new Stack<(HamtViewNode Left, HamtViewNode Right)>();
        pending.Push((Root, read.Index.Root));
        while (pending.TryPop(out var current))
        {
            if (current.Left.Leaf is { } left)
            {
                if (current.Right.Leaf is not { } right)
                    return new(false, []);
                comparisons.Add(new(left.Key, right.Key));
                comparisons.Add(new(left.Value, right.Value));
                continue;
            }
            if (current.Right.Leaf is not null || current.Left.Bitmap != current.Right.Bitmap
                || current.Left.IsCollision != current.Right.IsCollision
                || current.Left.Children.Count != current.Right.Children.Count)
                return new(false, []);
            for (int index = current.Left.Children.Count - 1; index >= 0; index--)
                pending.Push((current.Left.Children[index], current.Right.Children[index]));
        }

        return new(true, comparisons);
    }
}
