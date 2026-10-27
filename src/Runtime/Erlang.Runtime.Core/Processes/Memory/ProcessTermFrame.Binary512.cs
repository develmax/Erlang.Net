namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private ProcessBinaryFrame512? binaryRoots512;

    internal BitString AllocateBinaryInto512(int slot, int byteLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteLength, int.MaxValue / BitStringLayout.BitsPerByte);
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            BitString binary = BitString.FromOwnedBytes(new byte[byteLength]);
            PublishRoot512(slot, binary);

            return binary;
        }
    }

    // Caller holds owner.Gate. Stage both old/new actual backing leases first;
    // dictionary growth failures restore the previous publication atomically.
    private void PublishRoot512(int slot, Term value)
    {
        using ProcessBinaryPublication512 publication = (binaryRoots512 ??= owner.EnterBinaryFrame512()).Prepare512(slot, value);
        values[slot] = value;
        publication.Commit512();
    }
}
