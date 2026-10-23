namespace Erlang;

internal static class OrdsetCoreExecution
{
    internal static async ValueTask<Term> IsSet(ProcessContext context, Term set)
    {
        if (set is not Cons first)
            return CoreModules.Boolean(set is Nil);
        Term previous = first.Head;
        Term remaining = first.Tail;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            if (TermOrder.Compare(previous, cell.Head) >= 0)
                return CoreModules.Boolean(false);
            previous = cell.Head;
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw OrdsetCoreTrace.Clause(
                context,
                OrdsetCoreNames.IsSet,
                [remaining, previous],
                OrdsetCoreNames.IsSetHelperLine
            );

        return CoreModules.Boolean(true);
    }

    internal static async ValueTask<Term> Size(ProcessContext context, Term set)
    {
        try
        {
            return await ErlangListLength.ReadAsync(context, set);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            throw OrdsetCoreTrace.Length(context, set);
        }
    }

    internal static Term IsEqual(ProcessContext context, Term first, Term second)
    {
        if (first is not (Cons or Nil) || second is not (Cons or Nil))
            throw OrdsetCoreTrace.Clause(
                context,
                OrdsetCoreNames.IsEqual,
                [first, second],
                OrdsetCoreNames.IsEqualLine
            );

        using var comparison = RuntimeNumericMapComparison.EnterThread(context.Runtime);

        return CoreModules.Boolean(first.NumericEquals(second));
    }

    internal static async ValueTask<Term> IsElement(ProcessContext context, Term element, Term set)
    {
        Term remaining = set;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            int comparison = TermOrder.Compare(element, cell.Head);
            if (comparison <= 0)
                return CoreModules.Boolean(comparison == 0);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw OrdsetCoreTrace.Clause(
                context,
                OrdsetCoreNames.IsElement,
                [element, remaining],
                OrdsetCoreNames.IsElementLine
            );

        return CoreModules.Boolean(false);
    }

    internal static async ValueTask<Term> AddElement(ProcessContext context, Term element, Term set)
    {
        var prefix = new List<Term>();
        Term remaining = set;
        Term result;
        while (true)
        {
            await context.ReduceAsync();
            if (remaining is Nil)
            {
                result = new Cons(element, Nil.Value);
                break;
            }
            if (remaining is not Cons cell)
                throw OrdsetCoreTrace.Clause(
                    context,
                    OrdsetCoreNames.AddElement,
                    [element, remaining],
                    OrdsetCoreNames.AddElementLine,
                    prefix.Count
                );
            int comparison = TermOrder.Compare(element, cell.Head);
            if (comparison <= 0)
            {
                result = comparison == 0 ? remaining : new Cons(element, remaining);
                break;
            }
            prefix.Add(cell.Head);
            remaining = cell.Tail;
        }
        for (int index = prefix.Count - 1; index >= 0; index--)
            result = new Cons(prefix[index], result);

        return result;
    }
}
