namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private bool IsImmutableNativePublishedClosure441(Term value)
    {
        if (value is FunctionTerm or MapTerm or HamtBitmapTerm429 or ResourceReferenceTerm)
            return false;
        if (IsImmutableNoBackingClosure192(value) || IsImmutableLiteralOnlyClosure132(value))
            return true;
        if (CanSkipNativePublication441(value))
            return true;
        // Actual strict readonly ordinary SubBits/BinRef/range validation.
        // Writable/native-resource/origin-chain binaries cannot pass138.
        if (value is not BitString bits || !IsImmutableReadonlyCaptureClosure138(bits)
            || bits.OwnedStorage577 is not ProcessTermSequence506 block)
            return false;
        ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(block);

        return descriptor.Published && descriptor.BinRef.Published;
    }

    private bool CanSkipNativePublication441(Term value)
    {
        // Only actual immutable Cons/Tuple constructors set this flag. Keep
        // all binary retention, literal custody and collector edges unchanged.
        return Monitor.IsEntered(Gate) && value is Cons or TupleTerm
            && Storage(value) is ProcessTermSequence506 block
            && ReferenceEquals(block.Owner, this) && IsOwnedLocal(value)
            && !block.IsMutableSequenceToken814 && block.HasImmutableNativePublishedClosure441
            && CanUseImmutableClosure293(value, block);
    }
}
