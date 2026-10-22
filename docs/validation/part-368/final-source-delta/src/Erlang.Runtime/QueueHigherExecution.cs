using System.Numerics;

namespace Erlang;

internal static class QueueHigherExecution
{
    internal static ValueTask<Term> Run(ProcessContext context, string function, IReadOnlyList<Term> arguments)
        => function switch
        {
            QueueModuleNames.Member => QueueMemberExecution.Run(context, arguments[0], arguments[1]),
            QueueModuleNames.Reverse => ValueTask.FromResult(Reverse(context, arguments[0])),
            QueueModuleNames.Join => Join(context, arguments[0], arguments[1]),
            QueueModuleNames.Split => Split(context, arguments[0], arguments[1]),
            QueueModuleNames.Filter => QueueHigherFilters.Run(
                context,
                arguments[0],
                arguments[1],
                false
            ),
            QueueModuleNames.FilterMap => QueueHigherFilters.Run(
                context,
                arguments[0],
                arguments[1],
                true
            ),
            QueueModuleNames.Fold => Fold(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            ),
            QueueModuleNames.Any => Predicate(
                context,
                arguments[0],
                arguments[1],
                false
            ),
            QueueModuleNames.All => Predicate(
                context,
                arguments[0],
                arguments[1],
                true
            ),
            _ => QueueHigherDelete.Run(
                context,
                function,
                arguments[0],
                arguments[1]
            )
        };

    internal static bool TryQueue(Term value, out TupleTerm queue)
    {
        queue = (value as TupleTerm)!;

        return queue is { Items.Count: 2 } && queue.Items[0] is Cons or Nil && queue.Items[1] is Cons or Nil;
    }

    internal static bool True(Term value) => value is Atom { Name: ErlangBooleanAtoms.True };

    internal static bool False(Term value) => value is Atom { Name: ErlangBooleanAtoms.False };

    private static Term Reverse(ProcessContext context, Term value)
    {
        if (!TryQueue(value, out var queue))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Reverse,
                [value],
                QueueHigherSource.ReverseError
            );

        return Term.Tuple(queue.Items[1], queue.Items[0]);
    }

    private static async ValueTask<Term> Join(ProcessContext context, Term left, Term right)
    {
        if (!TryQueue(left, out var one) || !TryQueue(right, out var two))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Join,
                [left, right],
                QueueHigherSource.JoinError
            );
        if (two.Items[0] is Nil && two.Items[1] is Nil)
            return left;
        if (one.Items[0] is Nil && one.Items[1] is Nil)
            return right;
        Term reversed = await QueueCoreLinear.Reverse(
            context,
            one.Items[0],
            QueueModuleNames.Join,
            QueueHigherSource.JoinCall,
            2,
            two.Items[1]
        );
        Term front = await QueueCoreLinear.Append(
            context,
            one.Items[1],
            reversed,
            QueueModuleNames.Join,
            QueueHigherSource.JoinCall,
            2
        );

        return Term.Tuple(two.Items[0], front);
    }

    private static async ValueTask<Term> Split(ProcessContext context, Term index, Term value)
    {
        if (index is not Integer integer || integer.Value < 0 || !TryQueue(value, out var queue))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Split,
                [index, value],
                QueueHigherSource.SplitError
            );
        BigInteger n = integer.Value;
        Term rear = queue.Items[0], front = queue.Items[1];
        if (n.IsZero)
            return Term.Tuple(QueueOperations.New(), value);
        BigInteger frontLength = await QueueCoreLinear.Length(
            context,
            front,
            QueueModuleNames.Split,
            QueueHigherSource.SplitFrontLength,
            2
        );
        if (n < frontLength)
        {
            var first = (Cons)front;
            Term keepFront = Term.List(first.Head), moved = Nil.Value;
            front = first.Tail;
            for (BigInteger left = n - 1; left > 0; left--)
            {
                await context.ReduceAsync();
                var cell = (Cons)front;
                moved = new Cons(cell.Head, moved);
                front = cell.Tail;
            }

            return Term.Tuple(Term.Tuple(moved, keepFront), Term.Tuple(rear, front));
        }
        if (n > frontLength)
        {
            BigInteger rearLength = await QueueCoreLinear.Length(
                context,
                rear,
                QueueModuleNames.Split,
                QueueHigherSource.SplitRearLength,
                2
            );
            BigInteger m = rearLength - (n - frontLength);
            if (m < 0)
                throw QueueCoreTrace.Public(
                    context,
                    QueueModuleNames.Split,
                    [index, value],
                    QueueHigherSource.SplitRangeError
                );
            if (m.IsZero)
                return Term.Tuple(value, QueueOperations.New());
            var first = (Cons)rear;
            Term keepRear = Term.List(first.Head), moved = Nil.Value;
            rear = first.Tail;
            for (BigInteger left = m - 1; left > 0; left--)
            {
                await context.ReduceAsync();
                var cell = (Cons)rear;
                moved = new Cons(cell.Head, moved);
                rear = cell.Tail;
            }

            return Term.Tuple(Term.Tuple(rear, front), Term.Tuple(keepRear, moved));
        }
        Term firstQueue = await Balance(
            context,
            front,
            true,
            QueueModuleNames.Split,
            2
        );
        Term secondQueue = await Balance(
            context,
            rear,
            false,
            QueueModuleNames.Split,
            2
        );

        return Term.Tuple(firstQueue, secondQueue);
    }

    internal static async ValueTask<Term> Balance(
        ProcessContext context,
        Term list,
        bool frontToRear,
        string owner,
        int ownerArity
    )
    {
        if (list is Nil)
            return QueueOperations.New();
        if (list is Cons { Tail: Nil })
            return frontToRear ? Term.Tuple(list, Nil.Value) : Term.Tuple(Nil.Value, list);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return frontToRear ? Term.Tuple(Term.List(second.Head), Term.List(first.Head))
                : Term.Tuple(Term.List(first.Head), Term.List(second.Head));
        int line = frontToRear ? QueueHigherSource.FrontToRear : QueueHigherSource.RearToFront;
        BigInteger count = await QueueCoreLinear.Length(
            context,
            list,
            owner,
            line,
            ownerArity
        );
        var split = await QueueCoreLinear.Split(context, list, count / QueueCoreParameters.SplitDivisor);
        Term reversed = await QueueCoreLinear.Reverse(
            context,
            split.Remainder,
            owner,
            line,
            ownerArity
        );

        return frontToRear ? Term.Tuple(reversed, split.Prefix) : Term.Tuple(split.Prefix, reversed);
    }

    private static async ValueTask<Term> Fold(
        ProcessContext context,
        Term function,
        Term accumulator,
        Term value
    )
    {
        if (function is not FunctionTerm { Arity: 2 } callback || !TryQueue(value, out var queue))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Fold,
                [function, accumulator, value],
                QueueHigherSource.FoldError
            );
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module, QueueHigherSource.FoldFrontCall)))
            accumulator = await QueueHigherListCalls.Fold(
                context,
                callback,
                accumulator,
                queue.Items[1]
            );
        using var tail = context.EnterSourceTailCall(QueueModuleNames.Module, QueueModuleNames.Fold);

        return await ListFoldRight.Run(
            context,
            function,
            accumulator,
            queue.Items[0]
        );
    }

    private static async ValueTask<Term> Predicate(
        ProcessContext context,
        Term function,
        Term value,
        bool all
    )
    {
        string owner = all ? QueueModuleNames.All : QueueModuleNames.Any;
        if (function is not FunctionTerm { Arity: 1 } callback || !TryQueue(value, out var queue))
            throw QueueCoreTrace.Public(
                context,
                owner,
                [function, value],
                all ? QueueHigherSource.AllError : QueueHigherSource.AnyError
            );
        Term result;
        using (context.EnterSourceLocation(QueueHigherTrace.Location(QueueModuleNames.Module,
            all ? QueueHigherSource.AllFrontCall : QueueHigherSource.AnyFrontCall)))
            result = await QueueHigherListCalls.Predicate(
                context,
                callback,
                queue.Items[1],
                all
            );
        if (all ? !True(result) : True(result))
            return result;
        using var tail = context.EnterSourceTailCall(QueueModuleNames.Module, owner);

        return await QueueHigherListCalls.Predicate(
            context,
            callback,
            queue.Items[0],
            all
        );
    }
}
