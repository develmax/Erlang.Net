namespace Erlang;

internal sealed class ManagedCollectorOwnership413(
    ManagedLiteralStorageModel413 literalStorage,
    ManagedTraceStorageModel413 traceStorage
)
{
    internal bool Supported => literalStorage == ManagedLiteralStorageModel413.IndependentManagedTerms
        && traceStorage is ManagedTraceStorageModel413.ImmortalLegacySessionZero or ManagedTraceStorageModel413.IndependentManagedNamedSessions425;

    internal void RequireIndependentLiteralRoots()
    {
        if (literalStorage != ManagedLiteralStorageModel413.IndependentManagedTerms)
            throw new NotSupportedException(ManagedCollectorNames413.Invariant);
    }

    internal void RequireIndependentTraceRoots425()
    {
        if (traceStorage is not (ManagedTraceStorageModel413.ImmortalLegacySessionZero or ManagedTraceStorageModel413.IndependentManagedNamedSessions425))
            throw new NotSupportedException(ManagedCollectorNames413.Invariant);
    }

    internal bool SelectNextLiteralRegion()
    {
        // This representation has no borrowed/reclaimable region. A region
        // backend MUST supply real queues/barriers instead of using this path.
        RequireIndependentLiteralRoots();

        return false;
    }

    internal bool AdvanceTraceCleanupRound()
    {
        // Legacy0 is immortal. The named managed owner retires its entire
        // independent graph synchronously under the shared event gate; live
        // invocation records keep typed CLR owners, never borrowed raw pointers.
        // Thus there is no staged raw-pointer cleanup round in this profile.
        // NamedSessions without this concrete ownership proof remains refused.
        RequireIndependentTraceRoots425();

        return false;
    }
}
