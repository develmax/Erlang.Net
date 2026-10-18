// Modified: CLR adaptation of OTP-29.1.1 maps:iterator/next and erl_eval iterator validation.
namespace Erlang;

public static class MapIteration
{
    public static async ValueTask<Term> Iterator(Term map, Term order, ProcessContext context)
    {
        if (map is not MapTerm value)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (order is Atom atom)
            return Iterator(value, atom.Name);
        if (order is not FunctionTerm { Arity: 2 })
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var keys = Cons.From(value.Entries.Select(entry => entry.Key));
        var sorted = await ComparatorSort.Sort(order, keys, context);

        return new Cons(sorted, value);
    }

    public static Term Iterator(Term map, string order = MapIteratorAtoms.Unordered)
    {
        if (map is not MapTerm value)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), map));
        if (order == MapIteratorAtoms.Unordered)
            return new Cons(Term.I(MapIteratorLayout.InitialPath), value);
        if (order is not (MapIteratorAtoms.Ordered or MapIteratorAtoms.Reversed))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        IEnumerable<Term> keys = value.Entries.Select(entry => entry.Key);
        if (order == MapIteratorAtoms.Reversed)
            keys = keys.Reverse();

        return new Cons(Cons.From(keys), value);
    }

    public static Term Next(Term iterator) => Next(iterator, null);

    public static Term Next(Term iterator, ProcessRuntime? runtime)
    {
        if (iterator is TupleTerm { Items.Count: MapIteratorLayout.TupleArity })
            return iterator;
        if (iterator is Atom { Name: MapIteratorAtoms.None })
            return iterator;
        if (iterator is Cons { Tail: MapTerm map } state)
        {
            Term keys = state.Head;
            if (keys is Integer path)
            {
                if (HamtCursorDispatch.TryRead(
                    map,
                    path.Value,
                    runtime,
                    out Term cursor
                ))
                    return cursor;
                bool accepted = path.Value == MapIteratorLayout.InitialPath
                    || map.Entries.Count <= MapIteratorLayout.FlatMapMaximumEntries
                    && path.Value >= 0 && path.Value <= map.Entries.Count;
                if (!accepted)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var physicalEntries = map.Entries.Count <= MapIteratorLayout.FlatMapMaximumEntries
                    ? OtpMapHashLayoutProtocol.FlatEntries(map) : map.Entries;
                keys = Cons.From(physicalEntries.Select(entry => entry.Key));
            }
            var entries = new List<KeyValuePair<Term, Term>>();
            using var lookupScope = runtime is not null && map.Entries.Count > MapIteratorLayout.FlatMapMaximumEntries
                ? runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append) : null;
            var lookup = new HamtCursorLookup(map, lookupScope);
            foreach (Term key in Cons.Items(keys))
            {
                if (!lookup.TryGet(key, out var value))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                entries.Add(new(key, value!));
            }
            Term result = Term.A(MapIteratorAtoms.None);
            for (int i = entries.Count - 1; i >= 0; i--)
                result = Term.Tuple(entries[i].Key, entries[i].Value, result);

            return result;
        }

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    public static IReadOnlyList<KeyValuePair<Term, Term>> Entries(Term source) => Entries(source, null);

    public static IReadOnlyList<KeyValuePair<Term, Term>> Entries(Term source, ProcessRuntime? runtime)
    {
        if (source is MapTerm map)
            source = Iterator(map);
        var entries = new List<KeyValuePair<Term, Term>>();
        Term iterator = source;
        while (Next(iterator, runtime) is TupleTerm pair)
        {
            entries.Add(new(pair.Items[MapIteratorLayout.KeyIndex], pair.Items[MapIteratorLayout.ValueIndex]));
            iterator = pair.Items[MapIteratorLayout.ContinuationIndex];
        }

        return entries;
    }
}
