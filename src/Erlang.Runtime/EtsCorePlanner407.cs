namespace Erlang;

// Pure plan only. Caller must own actual snapshot/copy/atomic commit/retirement.
internal static class EtsCorePlanner407
{
    internal static Term Key(TupleTerm row, long position)
        => position > 0 && position <= row.Items.Count ? row.Items[(int)position - 1] : throw new ErlangException(ErlangErrorReasons.BadArgument);

    internal static bool SameKey(Term left, Term right, EtsCoreTableKind407 kind)
        => kind == EtsCoreTableKind407.OrderedSet ? TermOrder.Compare(left, right) == 0 : left.Equals(right);

    internal static IReadOnlyList<EtsCoreRow407>? Plan(
        IReadOnlyList<TupleTerm> current,
        IReadOnlyList<TupleTerm> incoming,
        EtsCoreTableKind407 kind,
        long keyPosition,
        bool onlyNew,
        CancellationToken cancellation
    )
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        foreach (var row in current) _ = Key(row, keyPosition);
        foreach (var row in incoming) _ = Key(row, keyPosition);
        if (onlyNew)
            foreach (var row in incoming)
                foreach (var old in current)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (SameKey(Key(row, keyPosition), Key(old, keyPosition), kind)) return null;
                }
        var next = Enumerable.Range(0, current.Count).Select(index => new EtsCoreRow407(false, index)).ToList();
        for (int index = 0; index < incoming.Count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            TupleTerm row = incoming[index];
            Term key = Key(row, keyPosition);
            bool duplicate = false;
            for (int oldIndex = next.Count - 1; oldIndex >= 0; oldIndex--)
            {
                cancellation.ThrowIfCancellationRequested();
                var source = next[oldIndex];
                var old = source.Incoming ? incoming[source.Index] : current[source.Index];
                if (!SameKey(key, Key(old, keyPosition), kind)) continue;
                if (kind is EtsCoreTableKind407.Set or EtsCoreTableKind407.OrderedSet) next.RemoveAt(oldIndex);
                else if (kind == EtsCoreTableKind407.Bag && row.Equals(old)) duplicate = true;
            }
            if (!duplicate) next.Add(new(true, index));
        }
        cancellation.ThrowIfCancellationRequested();

        return next.AsReadOnly();
    }
}
