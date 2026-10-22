using System.Numerics;

namespace Erlang;

public static class QueueOperations
{
    public static TupleTerm New() => Term.Tuple(Nil.Value, Nil.Value);

    public static Term IsQueue(Term value) => Boolean(value is TupleTerm { Items.Count: 2 } tuple && IsList(tuple.Items[0]) && IsList(tuple.Items[1]));

    public static Term IsEmpty(Term value)
    {
        var queue = RequireQueue(value);

        return Boolean(queue.Items[0] is Nil && queue.Items[1] is Nil);
    }

    public static Term Length(Term value)
    {
        var queue = RequireQueue(value);

        return new Integer(Count(queue.Items[0]) + Count(queue.Items[1]));
    }

    public static Term In(Term item, Term value)
    {
        var queue = RequireQueue(value);
        Term rear = queue.Items[0];
        Term front = queue.Items[1];
        if (rear is Cons { Tail: Nil } && front is Nil)
            return Term.Tuple(new Cons(item, Nil.Value), rear);

        return Term.Tuple(new Cons(item, rear), front);
    }

    public static Term Out(Term value)
    {
        if (value is not TupleTerm { Items.Count: 2 } queue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term rear = queue.Items[0];
        Term front = queue.Items[1];
        if (rear is Nil && front is Nil)
            return Term.Tuple(Term.A(QueueModuleNames.Empty), queue);
        if (front is Nil && rear is Cons { Tail: Nil } one)
            return Removed(one.Head, New());
        if (front is Nil && rear is Cons many)
        {
            var next = (Cons)Reverse(many.Tail);

            return Removed(next.Head, Term.Tuple(new Cons(many.Head, Nil.Value), next.Tail));
        }
        if (IsList(rear) && front is Cons first)
            return Removed(first.Head, first.Tail is Nil ? RearToFront(rear) : Term.Tuple(rear, first.Tail));
        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    public static Term FromList(Term list)
    {
        if (!IsList(list))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (list is Nil)
            return New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(list, Nil.Value);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(second.Head, Nil.Value), new Cons(first.Head, Nil.Value));
        var items = Cons.Items(list).ToArray();
        int half = items.Length / 2;

        return Term.Tuple(Cons.From(items.Skip(half).Reverse()), Cons.From(items.Take(half)));
    }

    public static Term ToList(Term value)
    {
        var queue = RequireQueue(value);
        Term reversed = Reverse(queue.Items[0]);

        return Cons.From(Cons.Items(queue.Items[1]), reversed);
    }

    private static TupleTerm RearToFront(Term list)
    {
        if (list is Nil)
            return New();
        if (list is Cons { Tail: Nil })
            return Term.Tuple(Nil.Value, list);
        if (list is Cons { Tail: Cons { Tail: Nil } second } first)
            return Term.Tuple(new Cons(first.Head, Nil.Value), new Cons(second.Head, Nil.Value));
        var items = Cons.Items(list).ToArray();
        int half = items.Length / 2;

        return Term.Tuple(Cons.From(items.Take(half)), Cons.From(items.Skip(half).Reverse()));
    }

    private static Term Removed(Term item, TupleTerm queue) => Term.Tuple(Term.Tuple(Term.A(QueueModuleNames.Value), item), queue);

    private static TupleTerm RequireQueue(Term value)
    {
        if (value is TupleTerm { Items.Count: 2 } queue && IsList(queue.Items[0]) && IsList(queue.Items[1]))
            return queue;
        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private static bool IsList(Term value) => value is Cons or Nil;

    private static Term Boolean(bool value) => CoreModules.Boolean(value);

    private static BigInteger Count(Term list)
    {
        BigInteger count = BigInteger.Zero;
        while (list is Cons cell)
        {
            count++;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return count;
    }

    private static Term Reverse(Term list)
    {
        Term result = Nil.Value;
        while (list is Cons cell)
        {
            result = new Cons(cell.Head, result);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return result;
    }
}
