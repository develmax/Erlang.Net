namespace Erlang;

public sealed partial class BitString
{
    internal PersistentBinarySlice416 CapturePersistentSlice416()
    {
        // Revoke the actual append writer before retaining its offheap storage.
        backing.Pin();

        return new(backing.IsReference ? new PersistentBinaryBacking416(backing) : null,
            backing.IsReference ? bitOffset : 0, BitLength);
    }

    internal static BitString FromPersistentBacking416(BinaryBacking storage, long offset, int count)
    {
        if (offset < 0 || count < 0 || offset > (long)storage.Length524 * BitStringLayout.BitsPerByte - count)
            throw new InvalidDataException(PersistentBinaryStorageNames416.InvalidRange);

        return new(storage, offset, count);
    }
}
