namespace Erlang;

internal static class QueueHigherDelete
{
    internal static async ValueTask<Term> Run(ProcessContext context, string function, Term probe, Term value)
    {
        bool predicate = function is QueueModuleNames.DeleteWith or QueueModuleNames.DeleteWithReverse;
        bool reverse = function is QueueModuleNames.DeleteReverse or QueueModuleNames.DeleteWithReverse;
        int error = predicate ? (reverse ? QueueHigherSource.DeleteWithReverseError : QueueHigherSource.DeleteWithError)
            : (reverse ? QueueHigherSource.DeleteReverseError : QueueHigherSource.DeleteError);
        if (!QueueHigherExecution.TryQueue(value, out var queue) || predicate && probe is not FunctionTerm { Arity: 1 })
            throw QueueCoreTrace.Public(context, function, [probe, value], error);
        if (!reverse)
            return await Delete(context, probe, queue, predicate);
        string inner = predicate ? QueueModuleNames.DeleteWith : QueueModuleNames.Delete;
        using var location = context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            predicate ? QueueHigherSource.DeleteWithReverseCall : QueueHigherSource.DeleteReverseCall));
        using var call = context.EnterSourceWrapper(QueueModuleNames.Module, inner, 2);
        var result = (TupleTerm)await Delete(context, probe, Term.Tuple(queue.Items[1], queue.Items[0]), predicate);

        return Term.Tuple(result.Items[1], result.Items[0]);
    }

    private static async ValueTask<Term> Delete(ProcessContext context, Term probe, TupleTerm queue, bool predicate)
    {
        string owner = predicate ? QueueModuleNames.DeleteWith : QueueModuleNames.Delete;
        Term front;
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            predicate ? QueueHigherSource.DeleteWithFrontCall : QueueHigherSource.DeleteFrontCall)))
            front = await Front(context, probe, queue.Items[1], predicate);
        if (!QueueHigherExecution.False(front))
            return front is Nil ? await QueueHigherExecution.Balance(context, queue.Items[0], false, owner, 2)
                : Term.Tuple(queue.Items[0], front);
        Term rear;
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            predicate ? QueueHigherSource.DeleteWithRearCall : QueueHigherSource.DeleteRearCall)))
            rear = await Rear(context, probe, queue.Items[0], predicate);
        if (QueueHigherExecution.False(rear))
            return queue;

        return rear is Nil ? await QueueHigherExecution.Balance(context, queue.Items[1], true, owner, 2)
            : Term.Tuple(rear, queue.Items[1]);
    }

    private static async ValueTask<Term> Front(ProcessContext context, Term probe, Term list, bool predicate)
    {
        string helper = predicate ? QueueHigherNames.DeleteWithFront : QueueHigherNames.DeleteFront;
        int recursiveLine = predicate ? QueueHigherSource.DeleteWithFrontRecursive : QueueHigherSource.DeleteFrontRecursive;
        var prefix = new List<Term>();
        Term result = Term.A(ErlangBooleanAtoms.False);
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            bool matches;
            if (predicate)
            {
                QueueHigherFrame[] history = QueueHigherTrace.Recursive(helper, recursiveLine, prefix.Count != 0);
                Term test = await QueueHigherTrace.Invoke(context, (FunctionTerm)probe, [cell.Head], QueueModuleNames.Module,
                    helper, 2, QueueHigherSource.DeleteWithFrontCallback, history);
                if (!QueueHigherExecution.True(test) && !QueueHigherExecution.False(test))
                    throw QueueHigherTrace.Case(context, QueueModuleNames.Module, helper, 2, test,
                        QueueHigherSource.DeleteWithFrontCallback, history);
                matches = QueueHigherExecution.True(test);
            }
            else
            {
                matches = probe.Equals(cell.Head);
            }
            if (matches)
            {
                result = cell.Tail;
                break;
            }
            prefix.Add(cell.Head);
            list = cell.Tail;
        }
        if (list is not (Cons or Nil))
            throw QueueHigherTrace.Clause(context, QueueModuleNames.Module, helper, [probe, list],
                predicate ? QueueHigherSource.DeleteWithFrontClause : QueueHigherSource.DeleteFrontClause,
                QueueHigherTrace.Recursive(helper, recursiveLine, prefix.Count != 0));
        if (QueueHigherExecution.False(result))
            return result;
        for (int index = prefix.Count - 1; index >= 0; index--)
        {
            await context.ReduceAsync();
            result = new Cons(prefix[index], result);
        }

        return result;
    }

    private static async ValueTask<Term> Rear(ProcessContext context, Term probe, Term list, bool predicate)
    {
        string helper = predicate ? QueueHigherNames.DeleteWithRear : QueueHigherNames.DeleteRear;
        int recursiveLine = predicate ? QueueHigherSource.DeleteWithRearRecursive : QueueHigherSource.DeleteRearRecursive;
        var pending = new Stack<Cons>();
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            pending.Push(cell);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw QueueHigherTrace.Clause(context, QueueModuleNames.Module, helper, [probe, list],
                predicate ? QueueHigherSource.DeleteWithRearClause : QueueHigherSource.DeleteRearClause,
                QueueHigherTrace.Recursive(helper, recursiveLine, pending.Count != 0));
        Term result = Term.A(ErlangBooleanAtoms.False);
        while (pending.TryPop(out Cons? cell))
        {
            await context.ReduceAsync();
            if (!QueueHigherExecution.False(result))
            {
                result = new Cons(cell.Head, result);
                continue;
            }
            bool matches;
            if (predicate)
            {
                QueueHigherFrame[] history = QueueHigherTrace.Recursive(helper, recursiveLine, pending.Count != 0);
                Term test = await QueueHigherTrace.Invoke(context, (FunctionTerm)probe, [cell.Head], QueueModuleNames.Module,
                    helper, 2, QueueHigherSource.DeleteWithRearCallback, history);
                if (!QueueHigherExecution.True(test) && !QueueHigherExecution.False(test))
                    throw QueueHigherTrace.Case(context, QueueModuleNames.Module, helper, 2, test,
                        QueueHigherSource.DeleteWithRearCallback, history);
                matches = QueueHigherExecution.True(test);
            }
            else
            {
                matches = probe.Equals(cell.Head);
            }
            if (matches)
                result = cell.Tail;
        }

        return result;
    }
}
