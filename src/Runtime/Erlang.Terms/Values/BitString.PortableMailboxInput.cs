namespace Erlang;

public sealed partial class BitString
{
    // Constructor-owned small managed storage only. Reject append writers,
    // reference/native backings, owned cells and derived resource views. No
    // native read, pin, callback, or capability is acquired by this predicate.
    internal bool IsPlainPortableMailboxInput
        => ownedHeapBits is null && ownedResourceBits577 is null
            && resourceSliceOrigin577 is null && writer is null
            && backing.NativeStorage524 is null && !backing.IsReference;
}
