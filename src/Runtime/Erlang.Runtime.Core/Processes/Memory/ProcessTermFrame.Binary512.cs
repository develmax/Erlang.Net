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
    private void PublishRoot512(int slot, Term value, bool finalNativeOwnership576 = true)
    {
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(owner, value);
        using var literalAdmission = owner.EnterLiteralAdmission801(literalRoots);
        if (literalRoots is not null) literalSlots801.EnsureCapacity(checked(literalSlots801.Count + 1));
        ModuleLiteralRoots801? preparedLiteral = literalRoots?.Retain801();
        bool literalPublished = false;
        try
        {
            using ProcessNativePublication524? native = owner.PrepareNativeHeaders524(value, finalNativeOwnership576);
            using ProcessBinaryPublication512 publication = (binaryRoots512 ??= owner.EnterBinaryFrame512()).Prepare512(slot, value);
            values[slot] = value;
            ModuleLiteralRoots801? previousLiteral = literalSlots801.GetValueOrDefault(slot);
            if (preparedLiteral is null) literalSlots801.Remove(slot);
            else literalSlots801[slot] = preparedLiteral;
            literalPublished = true;
            previousLiteral?.Dispose();
            // The actual root now owns its native headers even if retirement of
            // the previous backing lease reports a post-publication failure.
            native?.Commit524();
            publication.Commit512();
        }
        finally
        {
            if (!literalPublished) preparedLiteral?.Dispose();
        }
    }
}
