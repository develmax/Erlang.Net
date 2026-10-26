namespace Erlang;

internal static class QueueHigherFilters
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term function,
        Term value,
        bool map
    )
    {
        string owner = map ? QueueModuleNames.FilterMap : QueueModuleNames.Filter;
        if (function is not FunctionTerm { Arity: 1 } callback || !QueueHigherExecution.TryQueue(value, out var queue))
            throw QueueCoreTrace.Public(
                context,
                owner,
                [function, value],
                map ? QueueHigherSource.FilterMapError : QueueHigherSource.FilterError
            );
        Term front, rear;
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            map ? QueueHigherSource.FilterMapFrontCall : QueueHigherSource.FilterFrontCall)))
            front = await Front(
                context,
                callback,
                queue.Items[1],
                map
            );
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            map ? QueueHigherSource.FilterMapRearCall : QueueHigherSource.FilterRearCall)))
            rear = await Rear(
                context,
                callback,
                queue.Items[0],
                map
            );
        if (rear is Nil)
            return await QueueHigherExecution.Balance(
                context,
                front,
                true,
                owner,
                2
            );
        if (front is Nil)
            return await QueueHigherExecution.Balance(
                context,
                rear,
                false,
                owner,
                2
            );

        return Term.Tuple(rear, front);
    }

    private static async ValueTask<Term> Front(
        ProcessContext context,
        FunctionTerm function,
        Term list,
        bool map
    )
    {
        string module = map ? ListModuleNames.Module : QueueModuleNames.Module;
        string helper = map ? QueueHigherNames.ListFilterMap : QueueHigherNames.FilterFront;
        int callbackLine = map ? QueueHigherSource.ListFilterMapCallback : QueueHigherSource.FilterFrontCallback;
        var operations = new List<QueueHigherPending>();
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            QueueHigherFrame[] history = QueueHigherHistory.Read(operations, module, helper);
            Term result = await QueueHigherTrace.Invoke(
                context,
                function,
                [cell.Head],
                module,
                helper,
                2,
                callbackLine,
                history
            );
            if (QueueHigherExecution.True(result))
            {
                QueueHigherHistory.Add(
                    operations,
                    cell.Head,
                    QueueHigherEmission.Keep,
                    map ? QueueHigherSource.ListFilterMapKeep : QueueHigherSource.FilterFrontKeep
                );
            }
            else if (map && result is TupleTerm { Items.Count: 2 } pair && QueueHigherExecution.True(pair.Items[0]))
            {
                QueueHigherHistory.Add(
                    operations,
                    pair.Items[1],
                    QueueHigherEmission.Keep,
                    QueueHigherSource.ListFilterMapReplace
                );
            }
            else if (!map && result is Cons { Tail: Nil } replacement)
            {
                QueueHigherHistory.Add(
                    operations,
                    replacement.Head,
                    QueueHigherEmission.Keep,
                    QueueHigherSource.FilterFrontReplace
                );
            }
            else if (QueueHigherExecution.False(result) || !map && result is Nil)
            {
            }
            else if (!map && result is Cons)
            {
                QueueHigherHistory.Add(
                    operations,
                    result,
                    QueueHigherEmission.Expand,
                    QueueHigherSource.FilterFrontExpand
                );
            }
            else
            {
                throw QueueHigherTrace.Case(
                    context,
                    module,
                    helper,
                    2,
                    result,
                    callbackLine,
                    history
                );
            }
            list = cell.Tail;
        }
        if (list is not Nil)
            throw QueueHigherTrace.Clause(
                context,
                module,
                helper,
                [function, list],
                map ? QueueHigherSource.ListFilterMapHelper : QueueHigherSource.FilterFrontClause,
                QueueHigherHistory.Read(operations, module, helper)
            );
        Term output = Nil.Value;
        while (operations.Count != 0)
        {
            await context.ReduceAsync();
            QueueHigherPending item = operations[^1];
            operations.RemoveAt(operations.Count - 1);
            if (item.Kind == QueueHigherEmission.Keep)
            {
                output = new Cons(item.Value, output);
            }
            else
            {
                using var history = new QueueHigherScope(context, QueueHigherHistory.Read(operations, module, helper));
                output = await QueueCoreLinear.Append(
                    context,
                    item.Value,
                    output,
                    helper,
                    QueueHigherSource.FilterFrontExpand,
                    2
                );
            }
        }

        return output;
    }

    private static async ValueTask<Term> Rear(
        ProcessContext context,
        FunctionTerm function,
        Term list,
        bool map
    )
    {
        string helper = map ? QueueHigherNames.FilterMapRear : QueueHigherNames.FilterRear;
        int recursiveLine = map ? QueueHigherSource.FilterMapRearRecursive : QueueHigherSource.FilterRearRecursive;
        int callbackLine = map ? QueueHigherSource.FilterMapRearCallback : QueueHigherSource.FilterRearCallback;
        var pending = new Stack<Term>();
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            pending.Push(cell.Head);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw QueueHigherTrace.Clause(
                context,
                QueueModuleNames.Module,
                helper,
                [function, list],
                map ? QueueHigherSource.FilterMapRearClause : QueueHigherSource.FilterRearClause,
                QueueHigherTrace.Recursive(helper, recursiveLine, pending.Count != 0)
            );
        Term output = Nil.Value;
        while (pending.TryPop(out Term? item))
        {
            await context.ReduceAsync();
            QueueHigherFrame[] history = QueueHigherTrace.Recursive(helper, recursiveLine, pending.Count != 0);
            Term result = await QueueHigherTrace.Invoke(
                context,
                function,
                [item],
                QueueModuleNames.Module,
                helper,
                2,
                callbackLine,
                history
            );
            if (QueueHigherExecution.True(result))
                output = new Cons(item, output);
            else if (map && result is TupleTerm { Items.Count: 2 } pair && QueueHigherExecution.True(pair.Items[0]))
                output = new Cons(pair.Items[1], output);
            else if (!map && result is Cons { Tail: Nil } replacement)
                output = new Cons(replacement.Head, output);
            else if (QueueHigherExecution.False(result) || !map && result is Nil)
            {
            }
            else if (!map && result is Cons)
            {
                using var scope = new QueueHigherScope(context, history);
                output = await QueueCoreLinear.Reverse(
                    context,
                    result,
                    helper,
                    QueueHigherSource.FilterRearExpand,
                    2,
                    output
                );
            }
            else
                throw QueueHigherTrace.Case(
                    context,
                    QueueModuleNames.Module,
                    helper,
                    2,
                    result,
                    callbackLine,
                    history
                );
        }

        return output;
    }
}
