using System.Numerics;

namespace Erlang;

internal static class ListOperations
{
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
