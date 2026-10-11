namespace Erlang;

internal static class ComparatorUniqueSort
{
    public static async ValueTask<Term> Sort(ProcessContext context, Term function, Term list)
    {
        if (function is not FunctionTerm { Arity: 2 })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (list is Nil || list is Cons { Tail: Nil })
            return list;
        if (list is not Cons first || first.Tail is not Cons second)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        ValueTask<bool> Compare(Term left, Term right) => ListCallback.Compare(
            context,
            function,
            left,
            right
        );
        Term x = first.Head, y = second.Head, remaining = second.Tail;
        Term run = Nil.Value, runs = Nil.Value;
        bool ascending;
        while (true)
        {
            ascending = await Compare(x, y);
            if (!ascending || !await Compare(y, x))
                break;
            if (remaining is Nil)
                return new Cons(x, Nil.Value);
            if (remaining is not Cons cell)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            y = cell.Head;
            remaining = cell.Tail;
        }
        if (!ascending)
        {
            run = new Cons(x, Nil.Value);
            while (remaining is Cons cell)
            {
                Term z = cell.Head;
                remaining = cell.Tail;
                if (await Compare(y, z))
                {
                    if (await Compare(z, y))
                        continue;
                    runs = new Cons(ListOperations.Reverse(run, Nil.Value), Nil.Value);
                    run = Nil.Value;
                    x = y;
                    y = z;
                    ascending = true;

                    break;
                }
                run = new Cons(y, run);
                y = z;
            }
            if (!ascending)
            {
                if (remaining is not Nil)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);

                return new Cons(y, run);
            }
        }
        Term? pending = null;
        while (remaining is Cons cell)
        {
            Term z = cell.Head;
            remaining = cell.Tail;
            if (await Compare(y, z))
            {
                if (await Compare(z, y))
                    continue;
                run = new Cons(x, run);
                x = y;
                y = z;
            }
            else if (await Compare(x, z))
            {
                if (await Compare(z, x))
                    continue;
                run = new Cons(x, run);
                x = z;
            }
            else if (pending is not null)
            {
                bool before = await Compare(pending, z);
                if (before && await Compare(z, pending))
                    continue;
                runs = new Cons(new Cons(y, new Cons(x, run)), runs);
                x = before ? pending : z;
                y = before ? z : pending;
                pending = null;
                run = Nil.Value;
            }
            else if (run is Nil)
                run = new Cons(z, Nil.Value);
            else
                pending = z;
        }
        if (remaining is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        runs = new Cons(new Cons(y, new Cons(x, run)), runs);
        if (pending is not null)
            runs = new Cons(new Cons(pending, Nil.Value), runs);
        bool reverse = true;
        while (true)
        {
            Term accumulated = Nil.Value;
            while (runs is Cons one && one.Tail is Cons two)
            {
                Term left = reverse ? two.Head : one.Head;
                Term right = reverse ? one.Head : two.Head;
                accumulated = new Cons(await ListMergeOperations.MergeReversed(
                    left,
                    right,
                    reverse,
                    true,
                    Compare
                ), accumulated);
                runs = two.Tail;
            }
            if (runs is Cons last)
            {
                if (!reverse && accumulated is Nil)
                    return last.Head;
                accumulated = new Cons(ListOperations.Reverse(last.Head, Nil.Value), accumulated);
            }
            runs = accumulated;
            reverse = !reverse;
        }
    }
}
