using System.Numerics;

namespace Erlang;

internal static class QueueEndsBalance
{
    internal static async ValueTask<TupleTerm> RearToFront(ProcessContext context, Term list, string owner)
    {
        if (list is Nil)
            return QueueOperations.New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(Nil.Value, list);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(first.Head, Nil.Value), new Cons(second.Head, Nil.Value));
        BigInteger count = await QueueCoreLinear.Length(context, list, owner, QueueCoreSource.RearToFrontLength);
        var split = await QueueCoreLinear.Split(context, list, count / QueueCoreParameters.SplitDivisor);
        Term reversed = await QueueCoreLinear.Reverse(context, split.Remainder, owner, QueueCoreSource.RearToFrontLength);
        return Term.Tuple(split.Prefix, reversed);
    }

    internal static async ValueTask<TupleTerm> FrontToRear(ProcessContext context, Term list, string owner)
    {
        if (list is Nil)
            return QueueOperations.New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(list, Nil.Value);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(second.Head, Nil.Value), new Cons(first.Head, Nil.Value));
        BigInteger count = await QueueCoreLinear.Length(context, list, owner, QueueCoreSource.FrontToRearLength);
        var split = await QueueCoreLinear.Split(context, list, count / QueueCoreParameters.SplitDivisor);
        Term reversed = await QueueCoreLinear.Reverse(context, split.Remainder, owner, QueueCoreSource.FrontToRearLength);
        return Term.Tuple(reversed, split.Prefix);
    }
}
