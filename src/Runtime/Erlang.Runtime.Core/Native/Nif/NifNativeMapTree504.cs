namespace Erlang;

internal static class NifNativeMapTree504
{
    internal static NifNativeMapBranch504 FromView(
        HamtViewNode node,
        Func<KeyValuePair<Term, Term>, KeyValuePair<NifNativeTermValue, NifNativeTermValue>> read
    ) => node.Leaf is { } entry ? new(read(entry))
        : new(node.Bitmap, node.Children.Select(child => FromView(child, read)), node.IsCollision);

    internal static bool TryGet(
        NifNativeMapBranch504 node,
        Term key,
        ulong hash,
        out KeyValuePair<NifNativeTermValue, NifNativeTermValue> entry
    )
    {
        int level = 0;
        while (node.Entry is null)
        {
            if (node.Collision)
            {
                foreach (var child in node.Children)
                    if (NifNativeTermGraph.Resolve(child.Entry!.Value.Key).Equals(key))
                    {
                        entry = child.Entry.Value;

                        return true;
                    }
                entry = default;

                return false;
            }
            int digit = Digit(hash, level++);
            if ((node.Bitmap & (1 << digit)) == 0)
            {
                entry = default;

                return false;
            }
            node = node.Children[node.Slot(digit)];
        }
        entry = node.Entry.Value;

        return NifNativeTermGraph.Resolve(entry.Key).Equals(key);
    }

    internal static NifNativeMapBranch504 Put(
        NifNativeMapBranch504 node,
        NifNativeTermValue key,
        NifNativeTermValue value,
        Term resolvedKey,
        ulong hash,
        OtpMapRuntimeIdentityProvider hashes,
        bool updateOnly,
        out bool found,
        out bool added,
        int level = 0
    )
    {
        if (node.Entry is { } old)
        {
            found = NifNativeTermGraph.Resolve(old.Key).Equals(resolvedKey);
            added = !found;
            if (found)
                // erl_map.c insert_up constructs a new [argument_key|value]
                // leaf, unlike flat replacement which retains the key tuple.
                return SameValue(old.Value, value) ? node : new(new(key, value));
            if (updateOnly)
                return node;

            return Split(
                node,
                new(new(key, value)),
                hashes.Hash(NifNativeTermGraph.Resolve(old.Key)),
                hash,
                level
            );
        }
        if (node.Collision)
        {
            var children = node.Children.ToList();
            int index = 0;
            for (; index < children.Count; index++)
            {
                int comparison = TermOrder.Compare(resolvedKey, NifNativeTermGraph.Resolve(children[index].Entry!.Value.Key), true);
                if (comparison <= 0)
                {
                    found = comparison == 0;
                    added = !found;
                    if (found)
                    {
                        if (SameValue(children[index].Entry!.Value.Value, value))
                            return node;
                        children[index] = new(new(key, value));
                    }
                    else if (!updateOnly)
                        children.Insert(index, new(new(key, value)));

                    return found || !updateOnly ? new(0, children, true) : node;
                }
            }
            found = false;
            added = true;
            if (updateOnly)
                return node;
            children.Add(new(new(key, value)));

            return new(0, children, true);
        }
        int digit = Digit(hash, level);
        ushort bit = (ushort)(1 << digit);
        int slot = node.Slot(digit);
        var changed = node.Children.ToList();
        if ((node.Bitmap & bit) == 0)
        {
            found = false;
            added = true;
            if (updateOnly)
                return node;
            changed.Insert(slot, new(new(key, value)));

            return new((ushort)(node.Bitmap | bit), changed);
        }
        var child = Put(
            changed[slot],
            key,
            value,
            resolvedKey,
            hash,
            hashes,
            updateOnly,
            out found,
            out added,
            level + 1
        );
        if (ReferenceEquals(child, changed[slot]))
            return node;
        changed[slot] = child;

        return new(node.Bitmap, changed);
    }

    internal static NifNativeMapBranch504? Remove(
        NifNativeMapBranch504 node,
        Term key,
        ulong hash,
        out bool found,
        int level = 0
    )
    {
        if (node.Entry is { } entry)
        {
            found = NifNativeTermGraph.Resolve(entry.Key).Equals(key);

            return found ? null : node;
        }
        if (node.Collision)
        {
            var children = node.Children.ToList();
            int index = children.FindIndex(child => NifNativeTermGraph.Resolve(child.Entry!.Value.Key).Equals(key));
            found = index >= 0;
            if (!found)
                return node;
            children.RemoveAt(index);

            return children.Count == 1 ? children[0] : new(0, children, true);
        }
        int digit = Digit(hash, level);
        ushort bit = (ushort)(1 << digit);
        if ((node.Bitmap & bit) == 0)
        {
            found = false;

            return node;
        }
        int slot = node.Slot(digit);
        var changed = node.Children.ToList();
        var result = Remove(
            changed[slot],
            key,
            hash,
            out found,
            level + 1
        );
        if (!found)
            return node;
        ushort bitmap = node.Bitmap;
        if (result is null)
        {
            changed.RemoveAt(slot);
            bitmap ^= bit;
        }
        else
            changed[slot] = result;
        if (level != 0)
        {
            if (changed.Count == 0)
                return null;
            if (changed.Count == 1 && changed[0].Entry is not null)
                return changed[0];
        }

        return new(bitmap, changed);
    }

    private static NifNativeMapBranch504 Split(
        NifNativeMapBranch504 existing,
        NifNativeMapBranch504 inserted,
        ulong oldHash,
        ulong newHash,
        int level
    )
    {
        if (level == HamtViewLayout.HashLevels)
        {
            var children = TermOrder.Compare(NifNativeTermGraph.Resolve(inserted.Entry!.Value.Key),
                NifNativeTermGraph.Resolve(existing.Entry!.Value.Key), true) < 0
                ? new[] { inserted, existing } : new[] { existing, inserted };

            return new(0, children, true);
        }
        int previous = Digit(oldHash, level), next = Digit(newHash, level);
        if (previous == next)
            return new((ushort)(1 << next), [Split(
                existing,
                inserted,
                oldHash,
                newHash,
                level + 1
            )]);

        return new((ushort)((1 << previous) | (1 << next)),
            previous < next ? [existing, inserted] : [inserted, existing]);
    }

    private static int Digit(ulong hash, int level)
        => (int)((hash >> (level * HamtViewLayout.NibbleBits)) & HamtViewLayout.NibbleMask);

    internal static bool SameValue(NifNativeTermValue left, NifNativeTermValue right)
    {
        if (left.Pending is { } pending)
            return ReferenceEquals(pending, right.Pending);
        if (ReferenceEquals(left.StaticTerm, right.StaticTerm))
            return true;
        if (left.StaticTerm is Pid { LocalIdentity: not null } pid
            && right.StaticTerm is Pid otherPid
            && ReferenceEquals(pid.LocalIdentity, otherPid.LocalIdentity) && pid.Id == otherPid.Id)
            return true;
        if (left.StaticTerm is PortTerm { LocalIdentity: not null } port
            && right.StaticTerm is PortTerm otherPort
            && ReferenceEquals(port.LocalIdentity, otherPort.LocalIdentity) && port.Id == otherPort.Id)
            return true;

        // Small integers and atoms are immediate native words, even if their
        // hosting wrapper objects differ. Boxed equal objects are not the same word.
        return left.StaticTerm is Integer integer && right.StaticTerm is Integer other
            && integer.Value >= -(System.Numerics.BigInteger.One << HamtViewLayout.SmallValueBits)
            && integer.Value < (System.Numerics.BigInteger.One << HamtViewLayout.SmallValueBits)
            && integer.Value == other.Value
            || left.StaticTerm is Atom atom && right.StaticTerm is Atom otherAtom && atom.Name == otherAtom.Name;
    }
}
