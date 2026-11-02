namespace Erlang;

public sealed partial class BitString
{
    // This zero-byte sentinel carries no logical payload: every size/data read
    // of an owned heap binary is dispatched to its actual process heap cells.
    private static readonly BinaryBacking EmptyHeapBacking = new([], false);
    private readonly IOwnedHeapBitStringStorage? ownedHeapBits;
    internal IOwnedHeapBitStringStorage? OwnedHeapBits => ownedHeapBits;

    internal BitString(IOwnedHeapBitStringStorage storage)
    {
        backing = EmptyHeapBacking;
        ownedHeapBits = storage;
    }

    private BitString SliceOwnedHeapBits(int offset, int count)
        => new BitString(ownedHeapBits!.CopyHeapBytes(), BitLength).SliceBits(offset, count);
}
