using System.Numerics;

namespace Erlang;

internal static class HamtIteratorCursor
{
    internal static Term Read(
        MapTerm map,
        BigInteger path,
        Func<Term, ulong> hash,
        HamtViewNode? ownedRoot429 = null
    )
    {
        BigInteger limit = BigInteger.One << HamtViewLayout.SmallValueBits;
        bool small = path >= -limit && path < limit;
        if (!small && path.Sign < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger remaining = small ? new BigInteger(unchecked((ulong)(long)path) & HamtViewLayout.UnsignedSmallMask) : path;
        HamtViewNode current = ownedRoot429 ?? HamtCursorView.Branch(map.Entries.Select(pair => new HamtViewEntry(pair, hash(pair.Key))).ToArray(), 0);
        var positions = new List<(HamtViewNode Node, int Index)>();
        while (current.Leaf is null)
        {
            int index = (int)(remaining & HamtViewLayout.NibbleMask);
            if (index >= current.Children.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            positions.Add((current, index));
            current = current.Children[index];
            remaining >>= HamtViewLayout.NibbleBits;
        }
        int budget = path.IsZero ? HamtViewLayout.ReleaseFlatMapLimit : int.MaxValue;
        var values = new List<KeyValuePair<Term, Term>>();
        Term continuation = Term.A(MapIteratorAtoms.None);
        while (true)
        {
            if (positions.Count > HamtViewLayout.HashLevels && positions[^1].Index == 0)
                budget = Math.Max(budget, positions[^1].Node.Children.Count);
            values.Add(current.Leaf!.Value);
            budget--;
            bool found = false;
            while (positions.Count != 0)
            {
                int last = positions.Count - 1;
                var position = positions[last];
                int index = position.Index + 1;
                if (index >= position.Node.Children.Count)
                {
                    positions.RemoveAt(last);
                    continue;
                }
                positions[last] = (position.Node, index);
                current = position.Node.Children[index];
                while (current.Leaf is null)
                {
                    positions.Add((current, 0));
                    current = current.Children[0];
                }
                found = true;
                break;
            }
            if (!found)
                break;
            if (budget == 0)
            {
                BigInteger next = BigInteger.Zero;
                for (int depth = positions.Count - 1; depth >= 0; depth--)
                {
                    next <<= HamtViewLayout.NibbleBits;
                    next |= positions[depth].Index;
                }
                continuation = new Cons(new Integer(next), map);
                break;
            }
        }
        for (int index = values.Count - 1; index >= 0; index--)
            continuation = Term.Tuple(values[index].Key, values[index].Value, continuation);

        return continuation;
    }
}
