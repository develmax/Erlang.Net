namespace Erlang;

internal static class QueueHigherListCalls
{
    internal static async ValueTask<Term> Fold(ProcessContext context, FunctionTerm function, Term accumulator, Term list)
    {
        bool first = true;
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            accumulator = await QueueHigherTrace.Invoke(context, function, [cell.Head, accumulator],
                ListModuleNames.Module, first ? ListModuleNames.FoldLeft : QueueHigherNames.ListFoldLeft, 3,
                first ? QueueHigherSource.ListFoldCallback : QueueHigherSource.ListFoldHelperCallback, []);
            first = false;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw QueueHigherTrace.Clause(context, ListModuleNames.Module, QueueHigherNames.ListFoldLeft,
                [function, accumulator, list], QueueHigherSource.ListFoldHelper, []);

        return accumulator;
    }

    internal static async ValueTask<Term> Predicate(ProcessContext context, FunctionTerm function, Term list, bool all)
    {
        bool first = true;
        string publicName = all ? ListModuleNames.All : ListModuleNames.Any;
        string helper = all ? QueueHigherNames.ListAll : QueueHigherNames.ListAny;
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            string owner = first ? publicName : helper;
            int line = all ? (first ? QueueHigherSource.ListAllCallback : QueueHigherSource.ListAllHelperCallback)
                : (first ? QueueHigherSource.ListAnyCallback : QueueHigherSource.ListAnyHelperCallback);
            Term value = await QueueHigherTrace.Invoke(context, function, [cell.Head], ListModuleNames.Module, owner, 2, line, []);
            if (!QueueHigherExecution.True(value) && !QueueHigherExecution.False(value))
                throw QueueHigherTrace.Case(context, ListModuleNames.Module, owner, 2, value, line, []);
            if (all ? QueueHigherExecution.False(value) : QueueHigherExecution.True(value))
                return value;
            first = false;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw QueueHigherTrace.Clause(context, ListModuleNames.Module, helper, [function, list],
                all ? QueueHigherSource.ListAllHelper : QueueHigherSource.ListAnyHelper, []);

        return Term.A(all ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
    }
}
