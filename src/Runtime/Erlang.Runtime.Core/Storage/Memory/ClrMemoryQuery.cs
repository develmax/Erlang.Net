namespace Erlang;

internal static class ClrMemoryQuery
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term? query)
    {
        if (query is not null and not Atom and not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ClrMemorySnapshot snapshot = await ClrMemorySources.Capture(context.Cancellation);
        if (query is null)
            return snapshot.All();
        if (query is Atom)
            return snapshot.Select(query);
        var values = new List<Term>();
        while (query is Cons cell)
        {
            values.Add(Term.Tuple(cell.Head, snapshot.Select(cell.Head)));
            query = cell.Tail;
        }
        if (query is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return Term.List(values.ToArray());
    }
}
