using System.Numerics;

namespace Erlang;

internal static class ListBasicOperations
{
    public static IEnumerable<Term> Items(Term list)
    {
        while (list is Cons cell)
        {
            yield return cell.Head;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    public static Term Delete(Term item, Term list)
    {
        var prefix = new List<Term>();
        while (list is Cons cell)
        {
            if (cell.Head.Equals(item))
                return Cons.From(prefix, cell.Tail);
            prefix.Add(cell.Head);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Cons.From(prefix);
    }

    public static Term DropLast(Term list)
    {
        var result = new List<Term>();
        while (list is Cons cell)
        {
            if (cell.Tail is Nil)
                return Cons.From(result);
            result.Add(cell.Head);
            list = cell.Tail;
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    public static Term Prefix(Term prefix, Term list)
    {
        while (prefix is Cons first && list is Cons second && first.Head.Equals(second.Head))
        {
            prefix = first.Tail;
            list = second.Tail;
        }
        if (list is not (Cons or Nil) || prefix is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Term.A(prefix is Nil ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
    }

    public static Term Suffix(Term suffix, Term list)
    {
        int listLength = Cons.Items(list).Count();
        int suffixLength = Cons.Items(suffix).Count();

        return Term.A(listLength >= suffixLength && ListOperations.NthTail(Term.I(listLength - suffixLength), list).Equals(suffix) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
    }

    public static Term Sublist(Term list, Term length, Term? start = null)
    {
        if (start is not null)
        {
            if (start is not Integer index || index.Value < 1)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            BigInteger position = index.Value;
            while (position >= 2)
            {
                if (list is Nil)
                    return Nil.Value;
                if (list is not Cons cell)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                list = cell.Tail;
                position--;
            }
        }
        if (list is not (Cons or Nil) || length is not Integer count || count.Value < 0)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        BigInteger remaining = count.Value;
        var output = new List<Term>();
        while (remaining > 0 && list is Cons cell)
        {
            output.Add(cell.Head);
            list = cell.Tail;
            remaining--;
        }
        if (remaining > 0 && list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Cons.From(output);
    }

    public static Term Extreme(Term list, bool maximum)
    {
        if (list is not Cons first)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        Term result = first.Head;
        foreach (Term item in Items(first.Tail))
            if (maximum ? TermOrder.Compare(item, result) > 0 : TermOrder.Compare(item, result) < 0)
                result = item;

        return result;
    }

    public static Term Sum(Term list)
    {
        Term result = Term.I(0);
        foreach (Term item in Items(list))
            result = CoreModules.Arithmetic(ErlangOperators.Plus, result, item);

        return result;
    }

    public static Term FlatLength(Term list)
    {
        var pending = new Stack<Term>();
        pending.Push(list);
        BigInteger result = BigInteger.Zero;
        while (pending.TryPop(out var current))
            foreach (Term item in Items(current))
                if (item is Cons or Nil)
                    pending.Push(item);
                else
                    result++;

        return new Integer(result);
    }

    public static Term Enumerate(Term list, Term? start = null, Term? step = null)
    {
        start ??= Term.I(1);
        step ??= Term.I(1);
        if (start is not Integer index || step is not Integer increment)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        BigInteger position = index.Value;
        var result = new List<Term>();
        foreach (Term item in Items(list))
        {
            result.Add(Term.Tuple(new Integer(position), item));
            position += increment.Value;
        }

        return Cons.From(result);
    }

    public static Term Join(Term separator, Term list)
    {
        var result = new List<Term>();
        foreach (Term item in Items(list))
        {
            if (result.Count > 0)
                result.Add(separator);
            result.Add(item);
        }

        return Cons.From(result);
    }

    public static Term Subtract(Term list, Term removals)
    {
        var result = Cons.Items(list).ToList();
        foreach (Term item in Cons.Items(removals))
        {
            int position = result.FindIndex(value => value.Equals(item));
            if (position >= 0)
                result.RemoveAt(position);
        }

        return Cons.From(result);
    }

    public static Term Concat(Term list)
    {
        var parts = new List<Term>();
        foreach (Term item in Items(list))
            parts.Add(item switch
            {
                Integer number => Term.String(number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                FloatTerm number => Term.String(ListFloatFormatting.Format(number.Value)),
                Atom atom => Term.String(atom.Name),
                Cons or Nil => item,
                _ => throw new ErlangException(ErlangErrorReasons.FunctionClause)
            });
        Term result = Nil.Value;
        for (int i = parts.Count - 1; i >= 0; i--)
            result = Cons.From(Cons.Items(parts[i]), result);

        return result;
    }
}
