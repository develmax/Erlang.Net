namespace Erlang;

internal static class QueueMemberExecution
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term item, Term value)
    {
        if (value is not TupleTerm { Items.Count: QueueCoreParameters.TupleArity } queue
            || queue.Items[QueueCoreParameters.RearIndex] is not (Cons or Nil)
            || queue.Items[QueueCoreParameters.FrontIndex] is not (Cons or Nil))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Member,
                [item, value],
                QueueMemberSource.PublicError
            );
        bool found = await Search(
            context,
            item,
            queue.Items[QueueCoreParameters.RearIndex],
            true
        );
        if (!found)
            found = await Search(
                context,
                item,
                queue.Items[QueueCoreParameters.FrontIndex],
                true
            );

        return CoreModules.Boolean(found);
    }

    private static async ValueTask<bool> Search(
        ProcessContext context,
        Term item,
        Term list,
        bool retainQueueCaller
    )
    {
        while (true)
        {
            await context.ChargeAsync(0);
            if (list is Nil)
                return false;
            Term entry = list;
            long capacity = (long)context.RemainingListTraversalReductions * QueueMemberParameters.CellsPerReduction;
            long visited = 0;
            while (visited < capacity && list is Cons cell)
            {
                await context.ChargeAsync(0);
                visited++;
                if (ReferenceEquals(item, cell.Head) || item.Equals(cell.Head))
                {
                    await Charge(context, visited);

                    return true;
                }
                list = cell.Tail;
            }
            await Charge(context, visited);
            if (list is Nil)
                return false;
            if (list is not Cons)
                throw QueueCoreTrace.Dependency(
                    context,
                    ListModuleNames.Module,
                    QueueModuleNames.Member,
                    [item, entry],
                    QueueModuleNames.Member,
                    QueueMemberSource.SearchRear,
                    2,
                    retainQueueCaller
                );
        }
    }

    private static ValueTask Charge(ProcessContext context, long visited)
        => context.ChargeAsync((int)((visited + QueueMemberParameters.CellsPerReduction - 1)
            / QueueMemberParameters.CellsPerReduction));
}
