namespace Erlang;

internal static class OtpMapHashLayoutProtocol
{
    internal static void Append(
        MapTerm map,
        OtpMapHashState64 state,
        Func<Term, ulong> internalHash
    )
    {
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            AppendFlat(map, state);

            return;
        }
        HamtViewEntry[] entries = map.Entries
            .Select(pair => new HamtViewEntry(pair, internalHash(pair.Key)))
            .ToArray();
        HamtViewNode root = HamtCursorView.Branch(entries, 0);
        state.Alpha((ulong)(root.Children.Count == OtpMapHashLayoutNames.FullRootSlots
            ? OtpMapHashNames.HashMapHeadArray : OtpMapHashNames.HashMapHeadBitmap));
        state.Beta((ulong)map.Entries.Count);
        AppendChildren(root, 0, state);
    }

    private static void AppendFlat(MapTerm map, OtpMapHashState64 state)
    {
        state.Alpha(OtpMapHashNames.FlatMap);
        state.Beta((ulong)map.Entries.Count);
        if (map.Entries.Count == 0)
            return;
        KeyValuePair<Term, Term>[] entries = FlatEntries(map, state).ToArray();
        for (int index = 0; index + 1 < entries.Length; index++)
        {
            state.Push(entries[index].Value);
            state.Push(entries[index].Key);
        }
        state.Push(entries[^1].Value);
        state.Defer(entries[^1].Key);
    }

    internal static IReadOnlyList<KeyValuePair<Term, Term>> FlatEntries(MapTerm map)
    {
        var state = new OtpMapHashState64(new OtpMapOwnAtomIdentityProvider());

        return FlatEntries(map, state);
    }

    private static IReadOnlyList<KeyValuePair<Term, Term>> FlatEntries(MapTerm map, OtpMapHashState64 state)
    {
        return map.Entries
            .OrderBy(pair => pair.Key, Comparer<Term>.Create((left, right) => FlatKeyOrder(left, right, state)))
            .ToArray();
    }

    private static int FlatKeyOrder(Term left, Term right, OtpMapHashState64 state)
    {
        if (left is Atom && right is Atom)
        {
            if (!state.TryImmediateWord(left, out ulong leftWord)
                || !state.TryImmediateWord(right, out ulong rightWord))
                throw new ErlangException(Term.A(OtpMapHashLayoutNames.BadArgument));

            return leftWord.CompareTo(rightWord);
        }

        return TermOrder.Compare(left, right, true);
    }

    private static void AppendNode(HamtViewNode node, int level, OtpMapHashState64 state)
    {
        if (level == HamtViewLayout.HashLevels)
        {
            state.Alpha(OtpMapHashNames.Tuple);
            state.Beta((ulong)node.Children.Count);
            for (int index = 0; index + 1 < node.Children.Count; index++)
            {
                KeyValuePair<Term, Term> pair = node.Children[index].Leaf!.Value;
                state.Push(new Cons(pair.Key, pair.Value));
            }
            KeyValuePair<Term, Term> last = node.Children[^1].Leaf!.Value;
            state.Defer(new Cons(last.Key, last.Value));

            return;
        }
        state.Alpha(OtpMapHashNames.HashMapNode);
        AppendChildren(node, level, state);
    }

    private static void AppendChildren(HamtViewNode node, int level, OtpMapHashState64 state)
    {
        foreach (HamtViewNode child in node.Children)
        {
            if (child.Leaf is { } pair)
            {
                state.Push(pair.Value);
                state.Push(pair.Key);
            }
            else
            {
                state.DeferProtocol(next => AppendNode(child, level + 1, next));
            }
        }
    }
}
