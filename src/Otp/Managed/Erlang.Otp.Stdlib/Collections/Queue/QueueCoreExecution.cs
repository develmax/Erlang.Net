using System.Numerics;

namespace Erlang;

internal static class QueueCoreExecution
{
    internal static Term IsEmpty(ProcessContext context, Term value)
    {
        TupleTerm queue = Require(
            context,
            QueueModuleNames.IsEmpty,
            [value],
            value,
            QueueCoreSource.IsEmptyError
        );

        return CoreModules.Boolean(queue.Items[QueueCoreParameters.RearIndex] is Nil
            && queue.Items[QueueCoreParameters.FrontIndex] is Nil);
    }

    internal static Term In(ProcessContext context, Term item, Term value)
    {
        Require(
            context,
            QueueModuleNames.In,
            [item, value],
            value,
            QueueCoreSource.InError
        );

        return QueueOperations.In(item, value);
    }

    internal static async ValueTask<Term> Length(ProcessContext context, Term value)
    {
        TupleTerm queue = Require(
            context,
            QueueModuleNames.Length,
            [value],
            value,
            QueueCoreSource.LengthError
        );
        BigInteger rear = await QueueCoreLinear.Length(
            context,
            queue.Items[QueueCoreParameters.RearIndex],
            QueueModuleNames.Length,
            QueueCoreSource.LengthCall
        );
        BigInteger front = await QueueCoreLinear.Length(
            context,
            queue.Items[QueueCoreParameters.FrontIndex],
            QueueModuleNames.Length,
            QueueCoreSource.LengthCall
        );

        return new Integer(rear + front);
    }

    internal static async ValueTask<Term> Out(ProcessContext context, Term value)
    {
        if (value is not TupleTerm { Items.Count: QueueCoreParameters.TupleArity } queue)
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.Out,
                [value],
                QueueCoreSource.OutError
            );
        Term rear = queue.Items[QueueCoreParameters.RearIndex];
        Term front = queue.Items[QueueCoreParameters.FrontIndex];
        if (rear is Nil && front is Nil)
            return Term.Tuple(Term.A(QueueModuleNames.Empty), queue);
        if (front is Nil && rear is Cons { Tail: Nil } one)
            return Removed(one.Head, QueueOperations.New());
        if (front is Nil && rear is Cons many)
        {
            var reversed = (Cons)await QueueCoreLinear.Reverse(
                context,
                many.Tail,
                QueueModuleNames.Out,
                QueueCoreSource.OutReverseCall
            );

            return Removed(reversed.Head, Term.Tuple(new Cons(many.Head, Nil.Value), reversed.Tail));
        }
        if (rear is Cons or Nil && front is Cons first)
        {
            TupleTerm remaining = first.Tail is Nil
                ? await RearToFront(context, rear)
                : Term.Tuple(rear, first.Tail);

            return Removed(first.Head, remaining);
        }

        throw QueueCoreTrace.Public(
            context,
            QueueModuleNames.Out,
            [value],
            QueueCoreSource.OutError
        );
    }

    internal static async ValueTask<Term> FromList(ProcessContext context, Term list)
    {
        if (list is not (Cons or Nil))
            throw QueueCoreTrace.Public(
                context,
                QueueModuleNames.FromList,
                [list],
                QueueCoreSource.FromListError
            );
        if (list is Nil)
            return QueueOperations.New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(list, Nil.Value);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(second.Head, Nil.Value), new Cons(first.Head, Nil.Value));
        BigInteger count = await QueueCoreLinear.Length(
            context,
            list,
            QueueModuleNames.FromList,
            QueueCoreSource.FrontToRearLength
        );
        var split = await QueueCoreLinear.Split(context, list, count / QueueCoreParameters.SplitDivisor);
        Term reversed = await QueueCoreLinear.Reverse(
            context,
            split.Remainder,
            QueueModuleNames.FromList,
            QueueCoreSource.FrontToRearLength
        );

        return Term.Tuple(reversed, split.Prefix);
    }

    internal static async ValueTask<Term> ToList(ProcessContext context, Term value)
    {
        TupleTerm queue = Require(
            context,
            QueueModuleNames.ToList,
            [value],
            value,
            QueueCoreSource.ToListError
        );
        Term reversed = await QueueCoreLinear.Reverse(
            context,
            queue.Items[QueueCoreParameters.RearIndex],
            QueueModuleNames.ToList,
            QueueCoreSource.ToListCall
        );

        return await QueueCoreLinear.Append(
            context,
            queue.Items[QueueCoreParameters.FrontIndex],
            reversed,
            QueueModuleNames.ToList,
            QueueCoreSource.ToListCall
        );
    }

    private static async ValueTask<TupleTerm> RearToFront(ProcessContext context, Term list)
    {
        if (list is Nil)
            return QueueOperations.New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(Nil.Value, list);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(first.Head, Nil.Value), new Cons(second.Head, Nil.Value));
        BigInteger count = await QueueCoreLinear.Length(
            context,
            list,
            QueueModuleNames.Out,
            QueueCoreSource.RearToFrontLength
        );
        var split = await QueueCoreLinear.Split(context, list, count / QueueCoreParameters.SplitDivisor);
        Term reversed = await QueueCoreLinear.Reverse(
            context,
            split.Remainder,
            QueueModuleNames.Out,
            QueueCoreSource.RearToFrontLength
        );

        return Term.Tuple(split.Prefix, reversed);
    }

    private static TupleTerm Removed(Term item, TupleTerm queue) => Term.Tuple(Term.Tuple(Term.A(QueueModuleNames.Value), item), queue);

    private static TupleTerm Require(
        ProcessContext context,
        string name,
        IReadOnlyList<Term> arguments,
        Term value,
        int line
    )
    {
        if (value is TupleTerm { Items.Count: QueueCoreParameters.TupleArity } queue
            && queue.Items[QueueCoreParameters.RearIndex] is Cons or Nil
            && queue.Items[QueueCoreParameters.FrontIndex] is Cons or Nil)
            return queue;

        throw QueueCoreTrace.Public(
            context,
            name,
            arguments,
            line
        );
    }
}
