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

    public static Term Next(Term iterator)
    {
        if (iterator is TupleTerm { Items.Count: MapIteratorLayout.TupleArity })
            return iterator;
        if (iterator is Atom { Name: MapIteratorAtoms.None })
            return iterator;
        if (iterator is Cons { Tail: MapTerm map } state)
        {
            Term keys = state.Head;
            if (keys is Integer path && path.Value == MapIteratorLayout.InitialPath)
                keys = Cons.From(map.Entries.Select(entry => entry.Key));
            var entries = new List<KeyValuePair<Term, Term>>();
            foreach (Term key in Cons.Items(keys))
            {
                if (!map.TryGet(key, out var value))
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

    public static IReadOnlyList<KeyValuePair<Term, Term>> Entries(Term source)
    {
        if (source is MapTerm map)
            return map.Entries;
        var entries = new List<KeyValuePair<Term, Term>>();
        Term iterator = source;
        while (Next(iterator) is TupleTerm pair)
        {
            entries.Add(new(pair.Items[MapIteratorLayout.KeyIndex], pair.Items[MapIteratorLayout.ValueIndex]));
            iterator = pair.Items[MapIteratorLayout.ContinuationIndex];
        }

        return entries;
    }
}
