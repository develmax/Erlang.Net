namespace Erlang;

// Genuine own-storage protocol kernels. Native API publication is deliberately
// separate from this prerequisite; these bodies inspect the retained index used
// by production traversal, never an independent count-derived shadow graph.
internal static class HamtIndexProtocol429
{
    internal static Term Children(ProcessRuntime runtime, Term source)
    {
        runtime.RequireMapHashOpen();
        if (source is MapTerm map)
        {
            if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            using var read = runtime.AcquireHamtIndex429(map);

            return Cons.From(read.Index.Root.Children.Reverse().Select(child => child.StoredTerm!));
        }
        if (source is HamtBitmapTerm429 node)
        {
            runtime.RequireHamtNode429(node);

            return Cons.From(node.Children.Reverse());
        }
        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), source));
    }

    internal static Term Info(ProcessRuntime runtime, Term source)
    {
        if (source is not MapTerm map)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), source));
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var read = runtime.AcquireHamtIndex429(map);
        int[] leafUsage = new int[HamtViewLayout.HashLevels + 2];
        int[] bitmapUsage = new int[HamtViewLayout.NibbleMask + 1];
        int[] collisionUsage = new int[HamtViewLayout.NibbleMask + 1];
        int leafs = 0, bitmaps = 0, arrays = 0, collisions = 0, depth = 0;
        var stack = new Stack<(HamtViewNode Node, int Level, bool Root)>();
        stack.Push((read.Index.Root, 0, true));
        while (stack.TryPop(out var work))
        {
            if (work.Node.Leaf is not null)
            {
                leafs++;
                leafUsage[work.Level]++;
                continue;
            }
            if (work.Node.IsCollision)
            {
                collisions++;
                collisionUsage[Math.Min(work.Node.Children.Count, collisionUsage.Length) - 1]++;
            }
            else if (work.Root && work.Node.Children.Count == bitmapUsage.Length) arrays++;
            else
            {
                bitmaps++;
                bitmapUsage[work.Node.Children.Count - 1]++;
            }
            int next = work.Level + 1;
            depth = Math.Max(depth, next);
            foreach (var child in work.Node.Children) stack.Push((child, next, false));
        }

        Term Counts(int[] counts) => new TupleTerm(counts.Select(count => (Term)Term.I(count)).ToArray());

        return Term.List(
            Term.Tuple(Term.A(HamtProtocolNames429.Depth), Term.I(depth)),
            Term.Tuple(Term.A(HamtProtocolNames429.Arrays), Term.I(arrays), Nil.Value),
            Term.Tuple(Term.A(HamtProtocolNames429.Collisions), Term.I(collisions), Counts(collisionUsage)),
            Term.Tuple(Term.A(HamtProtocolNames429.Bitmaps), Term.I(bitmaps), Counts(bitmapUsage)),
            Term.Tuple(Term.A(HamtProtocolNames429.Leafs), Term.I(leafs), Counts(leafUsage))
        );
    }
}
