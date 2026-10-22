namespace Erlang;

internal static class QueueCoreReverse
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term list,
        string owner,
        int line,
        int ownerArity = 1,
        Term? initialTail = null,
        bool includeOwner = true
    )
    {
        Term tail = initialTail ?? Nil.Value;
        if (list is Nil)
            return tail;
        while (true)
        {
            await context.ChargeAsync(0);
            var entry = new QueueCoreReverseEntry(list, tail, context.RemainingListTraversalReductions);
            Term lookahead = entry.List;
            long cells = 0;
            while (cells < entry.CellBudget && lookahead is Cons cell)
            {
                await context.ChargeAsync(0);
                lookahead = cell.Tail;
                cells++;
            }
            await context.ChargeAsync((int)(cells / QueueCoreReverseParameters.CellsPerReduction));
            if (lookahead is not (Cons or Nil))
            {
                throw QueueCoreTrace.Dependency(
                    context,
                    ListModuleNames.Module,
                    ListModuleNames.Reverse,
                    [entry.List, entry.Tail],
                    owner,
                    line,
                    ownerArity,
                    includeOwner
                );
            }
            for (long copied = 0; copied < cells; copied++)
            {
                await context.ChargeAsync(0);
                var cell = (Cons)list;
                tail = new Cons(cell.Head, tail);
                list = cell.Tail;
            }
            if (list is Nil)
                return tail;
        }
    }
}
