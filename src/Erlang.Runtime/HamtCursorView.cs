using System.Numerics;

namespace Erlang;

internal static class HamtCursorView
{
    internal static Term ReadIntegerCursor(
        MapTerm map,
        BigInteger path,
        Func<Term, ulong> internalHash
    )
    {
        BigInteger smallLimit = BigInteger.One << HamtViewLayout.SmallValueBits;
        bool small = path >= -smallLimit && path < smallLimit;
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            if (!small || path.Sign < 0 || path > map.Entries.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return Cons.From(map.Entries.Select(pair => Term.Tuple(pair.Key, pair.Value)));
        }
        if (!small && path.Sign < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger remaining = small ? new BigInteger(unchecked((ulong)(long)path) & HamtViewLayout.UnsignedSmallMask) : path;
        HamtViewEntry[] entries = map.Entries.Select(pair => new HamtViewEntry(pair, internalHash(pair.Key))).ToArray();
        HamtViewNode root = Branch(entries, 0);
        var suffixes = new Stack<(HamtViewNode Node, int Next)>();
        HamtViewNode current = root;
        while (current.Leaf is null)
        {
            int index = (int)(remaining & HamtViewLayout.NibbleMask);
            if (index >= current.Children.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            suffixes.Push((current, index + 1));
            current = current.Children[index];
            remaining >>= HamtViewLayout.NibbleBits;
        }
        var result = new List<Term>();
        AppendLeaves(current, result);
        while (suffixes.TryPop(out var suffix))
        {
            for (int index = suffix.Next; index < suffix.Node.Children.Count; index++)
                AppendLeaves(suffix.Node.Children[index], result);
        }

        return Cons.From(result);
    }

    internal static HamtViewNode Branch(IReadOnlyList<HamtViewEntry> entries, int level)
    {
        if (entries.Count == 1)
            return new HamtViewNode(entries[0].Pair, []);
        if (level == HamtViewLayout.HashLevels)
        {
            HamtViewNode[] collisions = entries
                .OrderBy(entry => entry.Pair.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true)))
                .Select(entry => new HamtViewNode(entry.Pair, []))
                .ToArray();

            return new HamtViewNode(null, collisions);
        }
        HamtViewNode[] children = entries
            .GroupBy(entry => (entry.Hash >> (level * HamtViewLayout.NibbleBits)) & HamtViewLayout.NibbleMask)
            .OrderBy(group => group.Key)
            .Select(group => Branch(group.ToArray(), level + 1))
            .ToArray();

        return new HamtViewNode(null, children);
    }

    private static void AppendLeaves(HamtViewNode node, List<Term> result)
    {
        if (node.Leaf is { } pair)
        {
            result.Add(Term.Tuple(pair.Key, pair.Value));

            return;
        }
        foreach (HamtViewNode child in node.Children)
            AppendLeaves(child, result);
    }
}
