namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private bool IsImmutableReadonlyCaptureClosure138(Term value)
    {
        if (IsImmutableLiteralOnlyClosure132(value)) return true;
        if (IsImmutableNoBackingClosure192(value)) return true;
        if (Storage(value) is not ProcessTermSequence506 block || !ReferenceEquals(block.Owner, this)) return false;
        _ = IsOwnedLocal(value);
        if (value is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429)
            return block.HasImmutableReadonlyCaptureClosure138 && CanUseImmutableClosure293(value, block);
        if (value is not BitString bits || !ReferenceEquals(bits.OwnedStorage577, block)
            || bits.ResourceSliceOrigin577 is not null) return false;
        // Before the ordinary reader: a volatile writer reader can rebase and
        // rewrite a SubBits cell. This certificate must have NO such effects.
        ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(block);
        nuint rawFlags = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + ResourceBinary577Names.BaseWord)
            & ResourceBinary577Names.FlagMask;
        if (rawFlags != 0) return false;
        NativeOffheapHeader576 binRef = RequireNativeOffheapHeader576(descriptor.BinRef.Block, NativeOffheapKind576.BinaryReference);
        OrdinaryBinaryBacking? ordinary = binRef.OrdinaryBinary;
        if (!ReferenceEquals(descriptor.Owner, this) || !ReferenceEquals(descriptor.Block, block)
            || !ReferenceEquals(binRef, descriptor.BinRef) || !ReferenceEquals(binRef.Owner, this)
            || ordinary is null || !ReferenceEquals(ordinary.Runtime, runtime)
            || !ReferenceEquals(descriptor.Backing.NativeStorage524, ordinary)
            || !ReferenceEquals(bits.ResourceBacking577.NativeStorage524, ordinary)
            || ordinary.IsWritable || ordinary.WritableBinding is not null) return false;
        // The registered SubBits/BinRef/range/origin/native pointer must still
        // validate. With raw flags0 this reader cannot perform writer rebasing.
        _ = ReadResourceBits577(block);

        return true;
    }
}
