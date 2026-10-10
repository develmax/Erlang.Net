using System.Numerics;

namespace Erlang;

internal static class ListOperations
{
    public static Term Duplicate(Term count, Term value)
    {
        if (count is not Integer number || number.Value < 0)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (number.Value > ListDuplicateLimits.MaximumLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        Term result = Nil.Value;
        for (int remaining = (int)number.Value; remaining > 0; remaining--)
            result = new Cons(value, result);

        return result;
    }

    public static Term Flatten(Term list) => Flatten(list, Nil.Value);

    public static Term Flatten(Term list, Term tail)
    {
        if (list is not (Cons or Nil) || tail is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        var pending = new Stack<(Term Value, bool IsListSpine)>();
        pending.Push((list, true));
        Term result = tail;
        while (pending.TryPop(out var frame))
        {
            if (frame.Value is Nil)
                continue;
            if (frame.Value is Cons cell)
            {
                pending.Push((cell.Head, false));
                pending.Push((cell.Tail, true));

                continue;
            }
            if (frame.IsListSpine)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            result = new Cons(frame.Value, result);
        }

        return result;
    }

    public static Term Append(Term lists)
    {
        var prefixes = new List<Term>();
        while (lists is Cons cell)
        {
            if (cell.Tail is Nil)
            {
                Term result = cell.Head;
                for (int index = prefixes.Count - 1; index >= 0; index--)
                    result = Cons.From(Cons.Items(prefixes[index]), result);

                return result;
            }

            prefixes.Add(cell.Head);
            lists = cell.Tail;
        }
        if (lists is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Nil.Value;
    }

    public static Term Last(Term list)
    {
        if (list is not Cons first)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        Term last = first.Head;
        list = first.Tail;
        while (list is Cons cell)
        {
            last = cell.Head;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return last;
    }

    public static Term Split(Term count, Term list)
    {
        if (count is not Integer number || number.Value < 0 || list is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        BigInteger remaining = number.Value;
        Term reversed = Nil.Value;
        while (remaining > 0)
        {
            if (list is Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (list is not Cons cell)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            reversed = new Cons(cell.Head, reversed);
            list = cell.Tail;
            remaining--;
        }

        return Term.Tuple(Reverse(reversed, Nil.Value), list);
    }

    public static Term Reverse(Term list)
    {
        if (list is Nil || list is Cons { Tail: Nil })
            return list;
        if (list is not Cons { Tail: Cons second } first)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Reverse(second.Tail, new Cons(second.Head, new Cons(first.Head, Nil.Value)));
    }

    public static Term Reverse(Term list, Term tail)
    {
        while (list is Cons cell)
        {
            tail = new Cons(cell.Head, tail);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return tail;
    }

    public static Term KeyFind(Term key, Term position, Term list)
    {
        if (position is not Integer index || index.Value < 1 || index.Value > ListKeySearchLimits.MaximumSmallInteger)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        while (list is Cons cell)
        {
            if (cell.Head is TupleTerm tuple && index.Value <= tuple.Items.Count && KeyEquals(key, tuple.Items[(int)index.Value - 1]))
                return tuple;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.A(ListKeySearchAtoms.NotFound);
    }

    public static Term KeyMember(Term key, Term position, Term list) =>
        Term.A(KeyFind(key, position, list) is TupleTerm ? ListKeySearchAtoms.Found : ListKeySearchAtoms.NotFound);

    public static Term KeySearch(Term key, Term position, Term list)
    {
        Term result = KeyFind(key, position, list);

        return result is TupleTerm ? Term.Tuple(Term.A(ListKeySearchAtoms.Value), result) : result;
    }

    private static bool KeyEquals(Term key, Term element)
    {
        if (key is Integer number && number.Value >= ListKeySearchLimits.MinimumSmallInteger && number.Value <= ListKeySearchLimits.MaximumSmallInteger && element is FloatTerm floating)
        {
            number.TryToDouble(out double rounded);

            return rounded == floating.Value;
        }

        return key.NumericEquals(element);
    }

    public static Term Nth(Term index, Term list)
    {
        if (index is not Integer number || number.Value <= 0)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        BigInteger remaining = number.Value;
        while (list is Cons cell)
        {
            if (remaining == 1)
                return cell.Head;
            remaining--;
            list = cell.Tail;
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    public static Term NthTail(Term index, Term list)
    {
        if (index is not Integer number || number.Value < 0 || list is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        BigInteger remaining = number.Value;
        while (remaining > 0)
        {
            if (list is not Cons cell)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            list = cell.Tail;
            remaining--;
        }

        return list;
    }

    public static Term Sequence(Term first, Term last)
    {
        if (first is not Integer start || last is not Integer end || start.Value - 1 > end.Value)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return BuildSequence(start.Value, BigInteger.One, end.Value - start.Value + 1);
    }

    public static Term Sequence(Term first, Term last, Term increment)
    {
        if (first is not Integer start || last is not Integer end || increment is not Integer step)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (step.Value == 0)
        {
            if (start.Value != end.Value)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return new Cons(first, Nil.Value);
        }
        if (step.Value > 0 ? start.Value - step.Value > end.Value : start.Value - step.Value < end.Value)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        BigInteger count = (end.Value - start.Value + step.Value) / step.Value;

        return BuildSequence(start.Value, step.Value, count);
    }

    private static Term BuildSequence(BigInteger first, BigInteger step, BigInteger count)
    {
        if (count > ListSequenceLimits.MaximumLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        Term result = Nil.Value;
        BigInteger value = first + (count - 1) * step;
        for (int remaining = (int)count; remaining > 0; remaining--)
        {
            result = new Cons(new Integer(value), result);
            value -= step;
        }

        return result;
    }
}
