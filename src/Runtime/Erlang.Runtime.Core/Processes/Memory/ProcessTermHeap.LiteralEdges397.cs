namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private bool IsImmutableLiteralEdges397(Term value)
    {
        if (IsImmutableReadonlyCaptureClosure138(value)) return true;
        if (Storage(value) is ProcessTermSequence506 block)
        {
            if (!ReferenceEquals(block.Owner, this)) return false;
            _ = IsOwnedLocal(value);

            return value is Cons or TupleTerm && block.HasImmutableLiteralEdges397
                && CanUseImmutableClosure293(value, block);
        }

        // Exact sealed CLR leaf used by new_connection. Its Entry payload is
        // not a term edge in NativeMapOwnedRoots510/ReadChildren. This grants
        // ONLY stable literal traversal shape, never lifetime/copy authority.
        return value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.DistributionEntry } resource
            && Storage(resource) is null && ReferenceEquals(resource.Entry.Owner, runtime.ReferenceResources)
            && resource.Entry.IsAlive;
    }
}
