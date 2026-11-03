namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Capability is the exact private tuple owned by SequenceTraceStorage.
    // It is never returned as a language token and must not be used on an
    // arbitrary tuple. Label is immutable for this allocation's entire life.
    internal SequenceTraceToken RewriteSequenceToken(TupleTerm tuple, SequenceTraceToken previous, SequenceTraceToken next)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(previous.PhysicalTuple, tuple) ||
                tuple.OwnedStorage506 is not ProcessTermSequence506 block ||
                !IsOwnedLocal(tuple) || block.Count != SequenceTraceRootNames.Arity || block.HeaderWords526 != 1
                || block.HasImmutablePlainClosure550 || block.HasImmutableNoBackingClosure192
                || block.HasImmutableNoBinaryClosure192 || !block.IsMutableSequenceToken814 ||
                next.PhysicalTuple is { } nextTuple && !ReferenceEquals(nextTuple, tuple) ||
                !ReferenceEquals(previous.Label, next.Label) ||
                !ReferenceEquals(next.Sender, previous.Sender) && next.Sender is not Atom &&
                !(next.Sender is Pid pid && ReferenceEquals(pid.LocalIdentity, runtime.LocalIdentity)) ||
                (long)next.Flags < SequenceTraceParameters.MinimumSerial || (long)next.Flags > SequenceTraceParameters.MaximumSerial ||
                next.Serial < SequenceTraceParameters.MinimumSerial || next.Serial > SequenceTraceParameters.MaximumSerial ||
                next.LastCount < SequenceTraceParameters.MinimumSerial || next.LastCount > SequenceTraceParameters.MaximumSerial)
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            // All replaced edges are immediate; no new generational or native
            // resource edge can appear. Existing arbitrary Label stays intact.
            block.Slab.Cells.RewriteSequenceTokenCells(
                block.Offset,
                Term.I((long)next.Flags),
                Term.I(next.Serial),
                next.Sender,
                Term.I(next.LastCount)
            );

            return SequenceTraceToken.FromPhysical(tuple);
        }
    }
}
