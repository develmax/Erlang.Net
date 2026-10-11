namespace Erlang;

internal static class ListExtraFunctions
{
    public static Term Unique(Term list)
    {
        var seen = new HashSet<Term>();
        var output = new List<Term>();
        foreach (Term item in ListBasicOperations.Items(list))
            if (seen.Add(item))
                output.Add(item);

        return Cons.From(output);
    }

    public static async ValueTask<Term> Run(
        ProcessContext context,
        string operation,
        Term function,
        Term list
    )
    {
        if (function is not FunctionTerm { Arity: 1 } callback)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        var output = new List<Term>();
        var seen = new HashSet<Term>();
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            Term value = await callback.Invoke(context, [cell.Head]);
            if (operation == ListModuleNames.FlatMap)
                output.Add(value);
            else if (operation == ListModuleNames.Unique)
            {
                if (seen.Add(value))
                    output.Add(cell.Head);
            }
            else
            {
                if (value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
                    throw new ErlangException(Term.Tuple(Term.A(operation == ListModuleNames.Filter ? ErlangErrorReasons.BadFilter : ErlangErrorReasons.CaseClause), value));
                if (value is Atom { Name: ErlangBooleanAtoms.True })
                {
                    if (operation == ListModuleNames.Search)
                        return Term.Tuple(Term.A(ListKeySearchAtoms.Value), cell.Head);
                    output.Add(cell.Head);
                }
                else if (operation == ListModuleNames.SplitWith)
                    return Term.Tuple(Cons.From(output), list);
            }
            list = cell.Tail;
        }
        if (list is not Nil)
            throw operation == ListModuleNames.Filter ? new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), list)) : new ErlangException(ErlangErrorReasons.FunctionClause);
        if (operation == ListModuleNames.Search)
            return Term.A(ListKeySearchAtoms.NotFound);
        if (operation == ListModuleNames.SplitWith)
            return Term.Tuple(Cons.From(output), Nil.Value);
        if (operation == ListModuleNames.FlatMap)
        {
            Term result = Nil.Value;
            for (int i = output.Count - 1; i >= 0; i--)
                result = Cons.From(Cons.Items(output[i]), result);

            return result;
        }

        return Cons.From(output);
    }
}
