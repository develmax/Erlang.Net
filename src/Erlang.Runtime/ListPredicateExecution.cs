namespace Erlang;

internal static class ListPredicateExecution
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        string operation,
        Term function,
        Term list
    )
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, operation);
        bool all = operation == ListModuleNames.All;
        if (function is not FunctionTerm { Arity: 1 } callback)
            throw QueueHigherTrace.Clause(
                context,
                ListModuleNames.Module,
                operation,
                [function, list],
                all ? ListPredicateSource.AllClause : ListPredicateSource.AnyClause,
                []
            );
        if (list is not (Cons or Nil))
            throw QueueHigherTrace.Case(
                context,
                ListModuleNames.Module,
                operation,
                2,
                list,
                all ? ListPredicateSource.AllListCase : ListPredicateSource.AnyListCase,
                []
            );

        return await QueueHigherListCalls.Predicate(
            context,
            callback,
            list,
            all
        );
    }
}
