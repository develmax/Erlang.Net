// Modified: asynchronous, iterative adaptation of OTP-29.1.1 lists:sort/2.
namespace Erlang;

public static class ComparatorSort
{
    public static async ValueTask<Term> Sort(Term comparator, Term list, ProcessContext context)
    {
        if (list is Nil || list is Cons { Tail: Nil })
        {
            if (comparator is not FunctionTerm { Arity: 2 })
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            return list;
        }
        if (list is not Cons first || first.Tail is not Cons second)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        async ValueTask<bool> Compare(Term left, Term right)
        {
            await context.ReduceAsync();
            if (comparator is not FunctionTerm function)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), comparator));
            var result = await function.Invoke(context, [left, right]);
            if (result.Equals(Term.A(ErlangBooleanAtoms.True)))
                return true;
            if (result.Equals(Term.A(ErlangBooleanAtoms.False)))
                return false;

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));
        }

        bool ascending = await Compare(first.Head, second.Head);
        Term x = first.Head, y = second.Head, remaining = second.Tail;
        Term run = Nil.Value, runs = Nil.Value;
        Term? pending = null;
        while (remaining is Cons cell)
        {
            Term z = cell.Head;
            remaining = cell.Tail;
            if (await Compare(y, z) == ascending)
            {
                run = new Cons(x, run);
                x = y;
                y = z;
            }
            else if (await Compare(x, z) == ascending)
            {
                run = new Cons(x, run);
                x = z;
            }
            else if (pending is not null)
            {
                runs = new Cons(new Cons(y, new Cons(x, run)), runs);
                if (await Compare(pending, z) == ascending)
                {
                    x = pending;
                    y = z;
                }
                else
                {
                    x = z;
                    y = pending;
                }
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

        // fmergel and rfmergel alternate run direction and reverse the run stack.
        bool reverseMerge = ascending;
        while (true)
        {
            Term accumulated = Nil.Value;
            while (runs is Cons one && one.Tail is Cons two)
            {
                Term left = one.Head, right = two.Head;
                if (reverseMerge == ascending)
                    (left, right) = (right, left);
                accumulated = new Cons(await Merge(
                    left,
                    right,
                    reverseMerge,
                    Compare
                ), accumulated);
                runs = two.Tail;
            }
            if (runs is Cons last)
            {
                if (!reverseMerge && accumulated is Nil)
                    return last.Head;
                accumulated = new Cons(Reverse(last.Head, Nil.Value), accumulated);
            }
            runs = accumulated;
            reverseMerge = !reverseMerge;
        }
    }

    private static async ValueTask<Term> Merge(
        Term left,
        Term right,
        bool reverse,
        Func<Term, Term, ValueTask<bool>> compare
    )
    {
        Term result = Nil.Value;
        while (left is Cons first && right is Cons second)
        {
            bool takeLeft = await compare(first.Head, second.Head) != reverse;
            var chosen = takeLeft ? first : second;
            result = new Cons(chosen.Head, result);
            if (takeLeft)
                left = first.Tail;
            else
                right = second.Tail;
        }

        return Reverse(left is Nil ? right : left, result);
    }

    private static Term Reverse(Term list, Term tail)
    {
        while (list is Cons cell)
        {
            tail = new Cons(cell.Head, tail);
            list = cell.Tail;
        }

        return tail;
    }
}
