namespace Erlang;

internal static class NifNativeMapGraph504
{
    internal static bool TryEntries(
        NifNativeTermValue value,
        ProcessRuntime runtime,
        out IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> entries
    )
    {
        if (value.Pending is NifNativeMapNode504 pending)
        {
            entries = pending.Entries;

            return true;
        }
        if (TryNode(value, runtime, out var node))
        {
            entries = node.Entries;

            return true;
        }
        entries = Array.Empty<KeyValuePair<NifNativeTermValue, NifNativeTermValue>>();

        return false;
    }

    internal static bool TryNode(NifNativeTermValue value, ProcessRuntime runtime, out NifNativeMapNode504 node)
    {
        if (value.Pending is NifNativeMapNode504 pending)
        {
            node = pending;

            return true;
        }
        if (value.StaticTerm is not MapTerm map)
        {
            node = null!;

            return false;
        }
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            var entries = map.FlatKeys426!.Items.Select(key =>
            {
                var pair = map.Entries.First(entry => ReferenceEquals(entry.Key, key));

                return new KeyValuePair<NifNativeTermValue, NifNativeTermValue>(new(pair.Key), new(pair.Value));
            }).ToArray();
            node = new(entries);
        }
        else
        {
            using var index = runtime.AcquireHamtIndex429(map);
            node = new(NifNativeMapTree504.FromView(index.Index.Root,
                pair => new(new(pair.Key), new(pair.Value))), new(index.Index.Ownership));
        }

        return true;
    }

    internal static bool TryGet(
        NifNativeMapNode504 node,
        Term key,
        ProcessRuntime runtime,
        out KeyValuePair<NifNativeTermValue, NifNativeTermValue> result
    )
    {
        if (node.Root is { } root)
        {
            using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);

            return NifNativeMapTree504.TryGet(
                root,
                key,
                hashes.Hash(key),
                out result
            );
        }
        int index = Find(node.Entries, key);
        result = index < 0 ? default : node.Entries[index];

        return index >= 0;
    }

    internal static int Find(
        IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> entries,
        Term key
    )
    {
        for (int index = 0; index < entries.Count; index++)
            if (NifNativeTermGraph.Resolve(entries[index].Key).Equals(key))
                return index;

        return -1;
    }

    internal static NifNativeTermValue Create(
        IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> entries,
        ProcessRuntime runtime
    )
    {
        using var atoms = new AtomNameScope(runtime.AtomNames);
        if (entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
            return new(new NifNativeMapNode504(entries.OrderBy(entry => NifNativeTermGraph.Resolve(entry.Key),
                Comparer<Term>.Create(FlatCompare)).ToArray()));
        var captured = entries.ToArray();
        // Bulk construction hashes the current keys once. Dictionary compaction
        // also matches OTP's last-input-wins transition from a growing flat map.
        var ordering = new MapTerm(captured.Select((entry, index) =>
            new KeyValuePair<Term, Term>(NifNativeTermGraph.Resolve(entry.Key), Term.I(index))));
        if (ordering.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
            return new(new NifNativeMapNode504(ordering.FlatKeys426!.Items.Select(key =>
                captured[(int)((Integer)ordering.Get(key)).Value]).ToArray()));
        using var indexRead = runtime.AcquireHamtIndex429(ordering);
        var root = NifNativeMapTree504.FromView(indexRead.Index.Root,
            pair => captured[(int)((Integer)pair.Value).Value]);

        return new(new NifNativeMapNode504(root, new(indexRead.Index.Ownership)));
    }

    internal static NifNativeMapNode504 Change(
        NifNativeMapNode504 original,
        NifNativeTermValue key,
        NifNativeTermValue value,
        NifNativeMapMutation504Kind kind,
        ProcessRuntime runtime,
        out bool found
    )
    {
        using var atoms = new AtomNameScope(runtime.AtomNames);
        Term resolved = NifNativeTermGraph.Resolve(key);
        if (original.Root is { } root)
        {
            using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
            ulong hash = hashes.Hash(resolved);
            if (kind == NifNativeMapMutation504Kind.Remove)
            {
                var removed = NifNativeMapTree504.Remove(
                    root,
                    resolved,
                    hash,
                    out found
                );
                if (!found)
                    return original;
                var survivors = removed!.Entries().ToArray();
                if (survivors.Length <= HamtViewLayout.ReleaseFlatMapLimit)
                    return new(SortAfterRemoval(survivors));

                return new(removed, original.HashRetention!);
            }
            var changed = NifNativeMapTree504.Put(
                root,
                key,
                value,
                resolved,
                hash,
                hashes,
                kind == NifNativeMapMutation504Kind.Update,
                out found,
                out _
            );
            if (ReferenceEquals(changed, root))
                return original;

            return new(changed, new(runtime.RetainNativeMapHashes504(hashes), original.HashRetention));
        }
        int match = Find(original.Entries, resolved);
        found = match >= 0;
        if (found && kind != NifNativeMapMutation504Kind.Remove
            && NifNativeMapTree504.SameValue(original.Entries[match].Value, value))
            return original;
        if (!found && kind != NifNativeMapMutation504Kind.Put)
            return original;
        var entries = original.Entries.ToList();
        if (kind == NifNativeMapMutation504Kind.Remove)
            entries.RemoveAt(match);
        else if (found)
            entries[match] = new(entries[match].Key, value);
        else if (entries.Count == HamtViewLayout.ReleaseFlatMapLimit)
        {
            entries.Add(new(key, value));

            return (NifNativeMapNode504)Create(entries, runtime).Pending!;
        }
        else
        {
            int insertion = 0;
            while (insertion < entries.Count && FlatCompare(NifNativeTermGraph.Resolve(entries[insertion].Key), resolved) < 0)
                insertion++;
            entries.Insert(insertion, new(key, value));
        }

        return new(entries.ToArray());
    }

    private static int FlatCompare(Term left, Term right)
        => left is Atom first && right is Atom second
            ? AtomNameRegistry.IndexOf(first.Name).CompareTo(AtomNameRegistry.IndexOf(second.Name))
            : TermOrder.Compare(left, right, true);

    private static KeyValuePair<NifNativeTermValue, NifNativeTermValue>[] SortAfterRemoval(
        KeyValuePair<NifNativeTermValue, NifNativeTermValue>[] entries)
    {
        // erl_map.c validate_and_sort_flatmap uses insertion sort and stops at
        // an equal key. It does not silently collapse duplicate structural keys.
        for (int index = 1; index < entries.Length; index++)
            for (int slot = index; slot > 0; slot--)
            {
                int comparison = FlatCompare(NifNativeTermGraph.Resolve(entries[slot].Key),
                    NifNativeTermGraph.Resolve(entries[slot - 1].Key));
                if (comparison == 0)
                    return entries;
                if (comparison > 0)
                    break;
                (entries[slot], entries[slot - 1]) = (entries[slot - 1], entries[slot]);
            }

        return entries;
    }
}
