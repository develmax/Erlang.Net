namespace Erlang;

internal static class ListKeyOperations
{
    public static int Position(Term position)
    {
        if (position is not Integer number || number.Value < 1)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return number.Value > int.MaxValue ? int.MaxValue : (int)number.Value;
    }

    public static Term Element(Term tuple, int position)
    {
        if (tuple is not TupleTerm value || position > value.Items.Count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return value.Items[position - 1];
    }

    public static Term Change(
        string operation,
        Term key,
        Term position,
        Term list,
        Term? replacement = null
    )
    {
        int index = Position(position);
        if (replacement is not null && replacement is not TupleTerm)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        var prefix = new List<Term>();
        while (list is Cons cell)
        {
            if (cell.Head is TupleTerm tuple && index <= tuple.Items.Count && tuple.Items[index - 1].NumericEquals(key))
            {
                Term remaining = operation == ListModuleNames.KeyDelete || operation == ListModuleNames.KeyTake ? cell.Tail : new Cons(replacement!, cell.Tail);
                Term output = Cons.From(prefix, remaining);

                return operation == ListModuleNames.KeyTake ? Term.Tuple(Term.A(ListKeySearchAtoms.Value), cell.Head, output) : output;
            }
            prefix.Add(cell.Head);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (operation == ListModuleNames.KeyTake)
            return Term.A(ListKeySearchAtoms.NotFound);
        if (operation == ListModuleNames.KeyStore)
            prefix.Add(replacement!);

        return Cons.From(prefix);
    }

    public static async ValueTask<Term> Map(
        ProcessContext context,
        Term function,
        Term position,
        Term list
    )
    {
        var output = new List<Term>();
        while (list is Cons cell)
        {
            if (position is not Integer index || index.Value < 1 || index.Value > int.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            int n = (int)index.Value;
            Term argument = Element(cell.Head, n);
            await context.ReduceAsync();
            Term result = await ListCallback.Invoke(context, function, [argument]);
            var items = ((TupleTerm)cell.Head).Items.ToArray();
            items[n - 1] = result;
            output.Add(new TupleTerm(items));
            list = cell.Tail;
        }
        if (list is not Nil || function is not FunctionTerm { Arity: 1 })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        Position(position);

        return Cons.From(output);
    }
}
