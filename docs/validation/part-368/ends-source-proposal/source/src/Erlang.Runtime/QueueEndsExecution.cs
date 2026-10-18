namespace Erlang;

internal static class QueueEndsExecution
{
    internal static Term InRear(ProcessContext context, Term item, Term value)
    {
        TupleTerm queue = Require(context, value, QueueModuleNames.InReverse, QueueEndsSource.InRearError, [item, value]);
        Term rear = queue.Items[QueueCoreParameters.RearIndex];
        Term front = queue.Items[QueueCoreParameters.FrontIndex];
        if (rear is Nil && front is Cons { Tail: Nil })
            return Term.Tuple(front, new Cons(item, Nil.Value));
        return Term.Tuple(rear, new Cons(item, front));
    }

    internal static async ValueTask<Term> OutRear(ProcessContext context, Term value)
    {
        TupleTerm queue = Require(context, value, QueueModuleNames.OutReverse, QueueEndsSource.OutRearError);
        Term rear = queue.Items[QueueCoreParameters.RearIndex];
        Term front = queue.Items[QueueCoreParameters.FrontIndex];
        if (rear is Nil && front is Nil)
            return Term.Tuple(Term.A(QueueModuleNames.Empty), queue);
        if (rear is Nil && front is Cons { Tail: Nil } one)
            return Removed(one.Head, QueueOperations.New());
        if (rear is Nil && front is Cons many)
        {
            var reversed = (Cons)await QueueCoreLinear.Reverse(context, many.Tail, QueueModuleNames.OutReverse, QueueEndsSource.OutRearReverse);
            return Removed(reversed.Head, Term.Tuple(reversed.Tail, new Cons(many.Head, Nil.Value)));
        }
        var first = (Cons)rear;
        Term remaining = first.Tail is Nil
            ? await QueueEndsBalance.FrontToRear(context, front, QueueModuleNames.OutReverse)
            : Term.Tuple(first.Tail, front);
        return Removed(first.Head, remaining);
    }

    internal static async ValueTask<Term> Get(ProcessContext context, Term value, bool head)
    {
        string owner = head ? QueueModuleNames.Head : QueueModuleNames.Get;
        TupleTerm queue = Require(context, value, owner, head ? QueueEndsSource.HeadError : QueueEndsSource.GetError);
        Term rear = queue.Items[QueueCoreParameters.RearIndex];
        Term front = queue.Items[QueueCoreParameters.FrontIndex];
        if (front is Cons first)
            return first.Head;
        if (rear is Nil)
            throw QueueEndsTrace.Empty(context, owner, value, head ? QueueEndsSource.HeadEmpty : QueueEndsSource.GetEmpty);
        var cell = (Cons)rear;
        return cell.Tail is Nil ? cell.Head : await QueueEndsLast.Run(context, cell.Tail);
    }

    internal static async ValueTask<Term> GetRear(ProcessContext context, Term value)
    {
        TupleTerm queue = Require(context, value, QueueModuleNames.GetReverse, QueueEndsSource.GetRearError);
        Term rear = queue.Items[QueueCoreParameters.RearIndex];
        Term front = queue.Items[QueueCoreParameters.FrontIndex];
        if (rear is Cons first)
            return first.Head;
        if (front is Nil)
            throw QueueEndsTrace.Empty(context, QueueModuleNames.GetReverse, value, QueueEndsSource.GetRearEmpty);
        var cell = (Cons)front;
        return cell.Tail is Nil ? cell.Head : await QueueEndsLast.Run(context, cell.Tail);
    }

    internal static async ValueTask<Term> Peek(ProcessContext context, Term value, bool fromRear)
    {
        string owner = fromRear ? QueueModuleNames.PeekReverse : QueueModuleNames.Peek;
        TupleTerm queue = Require(context, value, owner, fromRear ? QueueEndsSource.PeekRearError : QueueEndsSource.PeekError);
        Term primary = queue.Items[fromRear ? QueueCoreParameters.RearIndex : QueueCoreParameters.FrontIndex];
        Term other = queue.Items[fromRear ? QueueCoreParameters.FrontIndex : QueueCoreParameters.RearIndex];
        if (primary is Cons first)
            return Term.Tuple(Term.A(QueueModuleNames.Value), first.Head);
        if (other is Nil)
            return Term.A(QueueModuleNames.Empty);
        var cell = (Cons)other;
        Term item = cell.Tail is Nil ? cell.Head : await QueueEndsLast.Run(context, cell.Tail, owner,
            fromRear ? QueueEndsSource.PeekRearLast : QueueEndsSource.PeekLast);
        return Term.Tuple(Term.A(QueueModuleNames.Value), item);
    }

    internal static async ValueTask<Term> Drop(ProcessContext context, Term value, bool fromRear)
    {
        string owner = fromRear ? QueueModuleNames.DropReverse : QueueModuleNames.Drop;
        TupleTerm queue = Require(context, value, owner, fromRear ? QueueEndsSource.DropRearError : QueueEndsSource.DropError);
        Term primary = queue.Items[fromRear ? QueueCoreParameters.RearIndex : QueueCoreParameters.FrontIndex];
        Term other = queue.Items[fromRear ? QueueCoreParameters.FrontIndex : QueueCoreParameters.RearIndex];
        if (primary is Nil)
        {
            if (other is Nil)
                throw QueueEndsTrace.Empty(context, owner, value, fromRear ? QueueEndsSource.DropRearEmpty : QueueEndsSource.DropEmpty);
            var first = (Cons)other;
            if (first.Tail is Nil)
                return QueueOperations.New();
            var reversed = (Cons)await QueueCoreLinear.Reverse(context, first.Tail, owner,
                fromRear ? QueueEndsSource.DropRearReverse : QueueEndsSource.DropReverse);
            Term kept = new Cons(first.Head, Nil.Value);
            return fromRear ? Term.Tuple(reversed.Tail, kept) : Term.Tuple(kept, reversed.Tail);
        }
        var selected = (Cons)primary;
        if (selected.Tail is Nil)
            return fromRear
                ? await QueueEndsBalance.FrontToRear(context, other, owner)
                : await QueueEndsBalance.RearToFront(context, other, owner);
        return fromRear ? Term.Tuple(selected.Tail, other) : Term.Tuple(other, selected.Tail);
    }

    private static TupleTerm Require(ProcessContext context, Term value, string owner, int line, IReadOnlyList<Term>? arguments = null)
    {
        if (value is TupleTerm { Items.Count: QueueCoreParameters.TupleArity } queue
            && queue.Items[QueueCoreParameters.RearIndex] is Cons or Nil
            && queue.Items[QueueCoreParameters.FrontIndex] is Cons or Nil)
            return queue;
        throw QueueCoreTrace.Public(context, owner, arguments ?? [value], line);
    }

    private static Term Removed(Term item, Term queue) => Term.Tuple(Term.Tuple(Term.A(QueueModuleNames.Value), item), queue);
}
