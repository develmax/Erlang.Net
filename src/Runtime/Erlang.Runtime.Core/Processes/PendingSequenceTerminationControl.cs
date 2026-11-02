namespace Erlang;

// Metadata belonging to an actual pending Entry's exact Admission delegate.
// Reason is covered by that Entry's existing source/native custody. This is
// neither a VM root nor invocation authority. Prepared owns real typed roots.
internal sealed class PendingSequenceTerminationControl(
    ProcessContext target,
    Term reason,
    Func<bool> willTerminate,
    Pid? removedLink,
    ProcessLinkIdentity? removedOrigin
)
{
    internal PreparedSequenceTermination? Prepared { get; private set; }

    internal void PrepareOutsideRuntimeGate()
    {
        target.Runtime.AssertSequenceTracePreparation();
        bool needed = target.Runtime.ReadPendingSequenceTerminationNeed(() => willTerminate()
            && target.Runtime.NeedsSequenceTerminationPreparation(target, removedLink, removedOrigin));
        Prepared = needed
            ? target.Runtime.PrepareSequenceTerminationAsync(
                target,
                reason,
                removedLink,
                removedOrigin
            ).GetAwaiter().GetResult()
            : null;
    }

    // Runtime R is held; no source state changes until original Admission.
    internal bool IsCurrent()
        => !willTerminate() || !target.Runtime.NeedsSequenceTerminationPreparation(target, removedLink, removedOrigin)
            || Prepared?.IsCurrentBeforeAdmission() == true;

    internal void ReleaseOutsideRuntimeGate()
    {
        target.Runtime.AssertSequenceTracePreparation();
        PreparedSequenceTermination? old = Prepared;
        Prepared = null;
        old?.DisposeAsync().GetAwaiter().GetResult();
    }
}
