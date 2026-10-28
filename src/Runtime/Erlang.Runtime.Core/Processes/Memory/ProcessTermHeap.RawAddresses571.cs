namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal ProcessTermRawAddress571 CaptureRawAddress571(
        ProcessTermSequence506 block,
        int relativeOffset,
        int words
    )
    {
        _ = ReadRawAddress571(block, relativeOffset, words);

        return new(
            this,
            block,
            relativeOffset,
            words
        );
    }

    internal nint ReadRawAddress571(ProcessTermSequence506 block, int relativeOffset, int words)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        ArgumentNullException.ThrowIfNull(block);
        // Owner and identity admission precede range or pointer reads. Reusing a
        // slab range cannot revive a capability for its retired block identity.
        if (!ReferenceEquals(block.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (!blocks.TryGetValue(block.Identity, out var active) || !ReferenceEquals(active, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        ArgumentOutOfRangeException.ThrowIfNegative(relativeOffset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(words);
        if (relativeOffset > block.AllocationWords526 || words > block.AllocationWords526 - relativeOffset)
            throw new ArgumentOutOfRangeException(nameof(words));

        return block.Slab.Cells.RawAddress571(checked(block.AllocationOffset526 + relativeOffset), words);
    }
}
