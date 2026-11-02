namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal TupleTerm CanonicalEmptyTuple803
    {
        get
        {
            TupleTerm value = runtime.EmptyTupleDomain803.Value803;
            _ = value.Items.Count; // Actual native arity/header read admission.

            return value;
        }
    }

    internal bool IsAuthenticatedEmptyTuple803(Term value)
    {
        if (value is not TupleTerm { OwnedStorage506: RuntimeEmptyTupleStorage803 storage }) return false;
        if (!ReferenceEquals(storage.Domain803, runtime.EmptyTupleDomain803)
            || !storage.Domain803.IsCanonical803(value))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);

        return true;
    }

    internal Term CanonicalizeEmptyTuple803(Term value)
    {
        if (value is TupleTerm { OwnedStorage506: RuntimeEmptyTupleStorage803 })
        {
            _ = IsAuthenticatedEmptyTuple803(value);

            return value;
        }

        return value is TupleTerm { OwnedStorage506: null, Items.Count: 0 } ? CanonicalEmptyTuple803 : value;
    }
}
