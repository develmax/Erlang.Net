namespace Erlang;

internal static class SequenceTraceLiteral814
{
    // Caller still owns the genuine state/queue-operation borrow while this
    // runs. No new process root or module admission is invented from a DTO.
    internal static ModuleLiteralRoots801? CapturePhysical(ProcessRuntime runtime, SequenceTraceToken token)
    {
        runtime.AssertSequenceTracePreparation();
        if (token.PhysicalTuple is not { OwnedStorage506: ProcessTermSequence506 block } tuple)
            return null;
        if (block.Owner is not ProcessTermHeap506 heap || !ReferenceEquals(heap.Runtime524, runtime))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        lock (heap.Gate)
        {
            if (!heap.IsOwnedLocal(tuple) || !block.IsMutableSequenceToken814)
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        }

        return ModuleLiteralRoots801.Capture801(heap, tuple);
    }
}
