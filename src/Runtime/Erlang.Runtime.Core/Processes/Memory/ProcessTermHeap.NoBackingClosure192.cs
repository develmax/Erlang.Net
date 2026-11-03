namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private bool IsImmutableNoBackingClosure192(Term value)
        => IsImmutableNoBackingClosure192(value, allowInlineHeapBits: true);

    private bool IsImmutableNoBinaryClosure192(Term value)
        => IsImmutableNoBackingClosure192(value, allowInlineHeapBits: false);

    private bool IsImmutableNoBackingClosure192(Term value, bool allowInlineHeapBits)
    {
        if (IsImmutablePlainClosure550(value)) return true;
        // This exact sealed CLR resource is a leaf in NativeMapOwnedRoots510.
        // It has no native magic/header/binary/typed-resource backing. The
        // actual DistributionEntry operation still validates owner/liveness;
        // this grants only immutable empty-backing traversal shape.
        if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.DistributionEntry } resource
            && Storage(resource) is null && ReferenceEquals(resource.Entry.Owner, runtime.ReferenceResources)
            && resource.Entry.IsAlive)
            return true;
        if (Storage(value) is not ProcessTermSequence506 block || !ReferenceEquals(block.Owner, this))
            return false;
        // Other-owner module literals remain lawful through their existing
        // real literal admission, but never receive this process-only proof.
        // Authenticate actual owner/activity BEFORE any numeric/binary reader.
        _ = IsOwnedLocal(value);
        if (value is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429)
            return CanUseImmutableClosure293(value, block) && (allowInlineHeapBits
                ? block.HasImmutableNoBackingClosure192 : block.HasImmutableNoBinaryClosure192);
        if (value is FloatTerm number && ReferenceEquals(number.OwnedStorage556, block))
        {
            _ = ReadFloat556(block);

            return true;
        }
        if (value is Integer integer && ReferenceEquals(integer.OwnedStorage559, block))
        {
            _ = ReadInteger559(block);

            return true;
        }
        if (allowInlineHeapBits && value is BitString bits && ReferenceEquals(bits.OwnedHeapBits, block)
            && bits.OwnedStorage577 is null && bits.ResourceSliceOrigin577 is null)
        {
            // Exact inline header/length/payload validation; no ordinary/refc,
            // resource or writable binary can pass through this storage family.
            _ = RequireHeapBitLength(block);

            return true;
        }

        return false;
    }
}
