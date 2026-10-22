namespace Erlang;

internal static class QueueEndsLast
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term list,
        string? owner = null,
        int line = 0
    )
    {
        if (list is not Cons first)
            throw QueueEndsTrace.LastClause(
                context,
                [list],
                QueueEndsSource.ListsLastEntry,
                owner,
                line
            );
        Term item = first.Head;
        Term tail = first.Tail;
        while (tail is Cons cell)
        {
            await context.ReduceAsync();
            item = cell.Head;
            tail = cell.Tail;
        }
        if (tail is Nil)
            return item;
        throw QueueEndsTrace.LastClause(
            context,
            [item, tail],
            QueueEndsSource.ListsLastLoop,
            owner,
            line
        );
    }
}
