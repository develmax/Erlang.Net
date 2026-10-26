namespace Erlang;

internal static class ListNativeTraversal
{
    internal static ValueTask<Term> Reverse(ProcessContext context, Term list, Term tail)
        => QueueCoreReverse.Run(
            context,
            list,
            ListModuleNames.Reverse,
            0,
            initialTail: tail,
            includeOwner: false
        );

    internal static async ValueTask<Term> Member(ProcessContext context, Term item, Term list)
        => CoreModules.Boolean(await QueueMemberExecution.Search(
            context,
            item,
            list,
            false
        ));
}
