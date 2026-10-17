namespace Erlang;

internal readonly struct NifNativeTermValue
{
    internal Term? StaticTerm { get; }
    internal NifNativeTermNode? Pending { get; }
    internal bool IsPending => Pending is not null;

    internal NifNativeTermValue(Term term) => StaticTerm = term;

    internal NifNativeTermValue(NifNativeTermNode pending) => Pending = pending;

    internal bool IsNil => StaticTerm is Nil;

    internal bool TryTupleArity(out int arity)
    {
        arity = StaticTerm is TupleTerm tuple ? tuple.Items.Count : Pending is NifNativeTupleNode pending ? pending.ChildCount : -1;

        return arity >= 0;
    }

    internal bool TryCons(out NifNativeTermValue head, out NifNativeTermValue tail)
    {
        if (StaticTerm is Cons cell)
        {
            head = new(cell.Head);
            tail = new(cell.Tail);

            return true;
        }
        if (Pending is NifNativeConsNode pending)
        {
            head = pending.Head;
            tail = pending.Tail;

            return true;
        }
        head = default;
        tail = default;

        return false;
    }

    internal Term FromCache(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
        => StaticTerm ?? cache[Pending!];
}
