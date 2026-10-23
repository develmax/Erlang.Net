namespace Erlang;

internal static class AllocatorQueryResultReducer
{
    internal static Term Initial(AllocatorQueryRequest request) => Cons.From(
        Cons.Items(request.Allocators).Select(allocator => Term.Tuple(allocator, Nil.Value))
    );

    internal static Term Merge(Term information, Term accumulated)
    {
        var prefix = new List<Term>();
        while (information is not Nil)
        {
            if (information is not Cons incoming || incoming.Head is not TupleTerm item ||
                item.Items.Count is not 2 and not 3 || accumulated is not Cons current ||
                current.Head is not TupleTerm { Items.Count: 2 } existing)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            Term allocator = item.Items[0];
            Term existingAllocator = existing.Items[0];
            if (!allocator.Equals(existingAllocator))
            {
                if (allocator.NumericEquals(existingAllocator))
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);

                prefix.Add(current.Head);
                accumulated = current.Tail;
                continue;
            }

            Term result;
            if (item.Items.Count == 2 && item.Items[1] is Atom { Name: AllocatorQueryNames.Disabled })
                result = item.Items[1];
            else if (item.Items.Count == 3)
                result = InsertInstance(item.Items[2], item.Items[1], existing.Items[1]);
            else
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            accumulated = new Cons(Term.Tuple(allocator, result), current.Tail);
            information = incoming.Tail;
        }

        return Cons.From(prefix, accumulated);
    }

    internal static Term Finish(AllocatorQueryRequest request, Term accumulated)
    {
        if (!request.Single)
            return accumulated;

        if (accumulated is Cons { Head: TupleTerm { Items.Count: 2 } item, Tail: Nil } &&
            request.Allocators is Cons { Head: Atom allocator } && item.Items[0].Equals(allocator))
            return item.Items[1];

        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), accumulated));
    }

    private static Term InsertInstance(Term information, Term instance, Term remaining)
    {
        if (instance is Atom)
            return new Cons(Term.Tuple(instance, information), remaining);

        var prefix = new List<Term>();
        while (remaining is Cons current)
        {
            if (current.Head is TupleTerm { Items.Count: 3 } existing &&
                existing.Items[0] is Atom { Name: AllocatorQueryNames.Instance } &&
                TermOrder.Compare(instance, existing.Items[1]) < 0)
                break;

            prefix.Add(current.Head);
            remaining = current.Tail;
        }
        if (remaining is not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Cons.From(prefix, new Cons(Term.Tuple(Term.A(AllocatorQueryNames.Instance), instance, information), remaining));
    }
}
