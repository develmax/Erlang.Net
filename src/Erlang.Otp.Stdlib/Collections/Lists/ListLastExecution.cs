namespace Erlang;

internal static class ListLastExecution
{
    internal static Term Read(ProcessContext context, Term list)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.Last);
        if (list is not Cons first)
            throw Failure(context, [list], ListLastSource.PublicClause);
        Term last = first.Head;
        Term reached = first.Tail;
        while (reached is Cons cell)
        {
            last = cell.Head;
            reached = cell.Tail;
        }
        if (reached is not Nil)
            throw Failure(context, [last, reached], ListLastSource.HelperClause);

        return last;
    }

    private static ErlangException Failure(ProcessContext context, IReadOnlyList<Term> arguments, int line)
        => RuntimeExceptionCapture.Call(
            context,
            new ErlangException(ErlangErrorReasons.FunctionClause),
            context.CaptureExecutionChain(),
            ListModuleNames.Module,
            ListModuleNames.Last,
            arguments,
            false,
            ListLastSource.Location(line)
        );
}
