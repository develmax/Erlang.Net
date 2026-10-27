using System.Numerics;

namespace Erlang;

internal static class HamtCursorView
{
    internal static Term ReadIntegerCursor(
        MapTerm map,
        BigInteger path,
        Func<Term, ulong> internalHash,
        HamtViewNode? ownedRoot429 = null,
        ProcessRuntime? runtime505 = null
    )
    {
        BigInteger smallLimit = BigInteger.One << HamtViewLayout.SmallValueBits;
        bool small = path >= -smallLimit && path < smallLimit;
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            if (!small || path.Sign < 0 || path > map.Entries.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return Cons.From(OtpMapHashLayoutProtocol.FlatEntries(map, runtime505)
                .Select(pair => Term.Tuple(pair.Key, pair.Value)));
        }
        if (!small && path.Sign < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger remaining = small ? new BigInteger(unchecked((ulong)(long)path) & HamtViewLayout.UnsignedSmallMask) : path;
        HamtViewNode root = ownedRoot429 ?? Branch(map.Entries.Select(pair => new HamtViewEntry(pair, internalHash(pair.Key))).ToArray(), 0);
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

    internal static Term ReadIntegerListCursor(
        MapTerm map,
        BigInteger path,
        Term accumulator,
        Func<Term, ulong> internalHash,
        int maximumEntries = int.MaxValue,
        HamtViewNode? ownedRoot429 = null,
        ProcessRuntime? runtime505 = null
    )
    {
        if (accumulator is not (Nil or Cons))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger smallLimit = BigInteger.One << HamtViewLayout.SmallValueBits;
        bool small = path >= -smallLimit && path < smallLimit;
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            if (!small || path.Sign < 0 || path > map.Entries.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var physical = OtpMapHashLayoutProtocol.FlatEntries(map, runtime505);
            for (int index = physical.Count - 1; index >= 0; index--)
                accumulator = new Cons(Term.Tuple(physical[index].Key, physical[index].Value), accumulator);

            return accumulator;
        }
        if (!small && path.Sign < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger remaining = small
            ? new BigInteger(unchecked((ulong)(long)path) & HamtViewLayout.UnsignedSmallMask)
            : path;
        HamtViewNode current = ownedRoot429 ?? Branch(map.Entries.Select(pair => new HamtViewEntry(pair, internalHash(pair.Key))).ToArray(), 0);
        var positions = new List<(HamtViewNode Node, int Index)>();
        while (current.Leaf is null)
        {
            int offset = (int)(remaining & HamtViewLayout.NibbleMask);
            int index = current.Children.Count - 1 - offset;
            if (index < 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            positions.Add((current, index));
            current = current.Children[index];
            remaining >>= HamtViewLayout.NibbleBits;
        }
        int budget = Math.Max(maximumEntries, 1);
        if (path.IsZero)
            budget = Math.Min(budget, HamtViewLayout.ReleaseFlatMapLimit);
        while (true)
        {
            // Branch creates hash-collision nodes at HashLevels, after ordinary hash digits.
            // OTP extends the budget at the end of a collision node to avoid yielding inside it.
            if (positions.Count > HamtViewLayout.HashLevels)
            {
                var collision = positions[^1];
                if (collision.Index == collision.Node.Children.Count - 1)
                    budget = Math.Max(budget, collision.Node.Children.Count);
            }
            var pair = current.Leaf!.Value;
            accumulator = new Cons(Term.Tuple(pair.Key, pair.Value), accumulator);
            budget--;
            bool found = false;
            while (positions.Count > 0)
            {
                int last = positions.Count - 1;
                var position = positions[last];
                int index = position.Index - 1;
                if (index < 0)
                {
                    positions.RemoveAt(last);
                    continue;
                }
                positions[last] = (position.Node, index);
                current = position.Node.Children[index];
                while (current.Leaf is null)
                {
                    int child = current.Children.Count - 1;
                    positions.Add((current, child));
                    current = current.Children[child];
                }
                found = true;
                break;
            }
            if (!found)
                return accumulator;
            if (budget == 0)
            {
                BigInteger next = BigInteger.Zero;
                for (int depth = positions.Count - 1; depth >= 0; depth--)
                {
                    var position = positions[depth];
                    next <<= HamtViewLayout.NibbleBits;
                    next |= position.Node.Children.Count - 1 - position.Index;
                }

                return new Cons(new Integer(next), new Cons(map, accumulator));
            }
        }
    }

    internal static HamtViewNode Branch(
        IReadOnlyList<HamtViewEntry> entries,
        int level,
        object? domain = null,
        object? ownership = null
    )
    {
        if (entries.Count == 1)
            return new HamtViewNode(
                entries[0].Pair,
                [],
                domain: domain,
                ownership: ownership
            );
        if (level == HamtViewLayout.HashLevels)
        {
            HamtViewNode[] collisions = entries
                .OrderBy(entry => entry.Pair.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true)))
                .Select(entry => new HamtViewNode(
                    entry.Pair,
                    [],
                    domain: domain,
                    ownership: ownership
                ))
                .ToArray();

            return new HamtViewNode(
                null,
                collisions,
                collision: true,
                domain: domain,
                ownership: ownership
            );
        }
        var groups = entries
            .GroupBy(entry => (entry.Hash >> (level * HamtViewLayout.NibbleBits)) & HamtViewLayout.NibbleMask)
            .OrderBy(group => group.Key).ToArray();
        ushort bitmap = 0;
        foreach (var group in groups) bitmap |= (ushort)(1 << (int)group.Key);
        HamtViewNode[] children = groups.Select(group => Branch(
            group.ToArray(),
            level + 1,
            domain,
            ownership
        )).ToArray();

        return new HamtViewNode(
            null,
            children,
            bitmap,
            domain: domain,
            ownership: ownership,
            head: level == 0
        );
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
