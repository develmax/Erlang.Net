namespace Erlang;

internal static class ErlangTupleBifs
{
    public static Term FromList(Term value)
    {
        int count = 0;
        Term tail = value;
        while (tail is Cons cell)
        {
            if (count == ErlangTupleLimits.MaximumArity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            count++;
            tail = cell.Tail;
        }
        if (tail is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new TupleTerm(Cons.Items(value));
    }

    public static int Index(Term value, int maximum)
    {
        if (value is not Integer integer || integer.Value < 1 || integer.Value > maximum)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (int)integer.Value - 1;
    }

    public static Term Change(string operation, IReadOnlyList<Term> arguments)
    {
        int tupleArgument = operation == ErlangBasicBifNames.AppendElement ? 0 : 1;
        if (arguments[tupleArgument] is not TupleTerm tuple)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (operation == ErlangBasicBifNames.AppendElement && tuple.Items.Count >= ErlangTupleLimits.MaximumArity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var items = tuple.Items.ToList();
        if (operation == ErlangBasicBifNames.AppendElement)
            items.Add(arguments[1]);
        else
        {
            int index = Index(arguments[0], items.Count + (operation == ErlangBasicBifNames.InsertElement ? 1 : 0));
            if (operation == ErlangBasicBifNames.DeleteElement)
                items.RemoveAt(index);
            else if (operation == ErlangBasicBifNames.InsertElement)
                items.Insert(index, arguments[2]);
            else
                items[index] = arguments[2];
        }

        return new TupleTerm(items);
    }

    public static Term Create(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Integer size || size.Value < 0 || size.Value > ErlangTupleLimits.MaximumArity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var items = Enumerable.Repeat(arguments[1], (int)size.Value).ToArray();
        if (arguments.Count == 3)
            foreach (Term item in Cons.Items(arguments[2]))
            {
                if (item is not TupleTerm { Items.Count: 2 } pair)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                items[Index(pair.Items[0], items.Length)] = pair.Items[1];
            }

        return new TupleTerm(items);
    }
}
