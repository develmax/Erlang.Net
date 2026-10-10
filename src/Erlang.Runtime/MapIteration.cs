// Modified: CLR adaptation of OTP-29.1.1 maps:iterator/next and erl_eval iterator validation.
namespace Erlang;

public static class MapIteration
{
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
            if (keys is Nil)
                return Term.A(MapIteratorAtoms.None);
            if (keys is Cons key && map.TryGet(key.Head, out var value))
                return Term.Tuple(key.Head, value!, new Cons(key.Tail, map));
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
