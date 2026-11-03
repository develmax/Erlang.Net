using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly ConditionalWeakTable<Func<bool?>, PendingSequenceTerminationControl> pendingSequenceTermination = new();

    private PendingSequenceTerminationControl CreatePendingSequenceTerminationControl(
        ProcessContext target,
        Term reason,
        Func<bool> willTerminate,
        Pid? removedLink = null,
        ProcessLinkIdentity? removedOrigin = null
    ) => new(
        target,
        reason,
        willTerminate,
        removedLink,
        removedOrigin
    );

    private void RegisterPendingSequenceTermination(Func<bool?> admission, PendingSequenceTerminationControl control)
        => pendingSequenceTermination.Add(admission, control);

    private PendingSequenceTerminationControl? FindPendingSequenceTermination(MailboxPendingBackingClaim claim)
        => claim.Original.Admission is { } admission && pendingSequenceTermination.TryGetValue(admission, out var control)
            ? control : null;

    internal bool ReadPendingSequenceTerminationNeed(Func<bool> predicate)
    {
        lock (gate) return predicate();
    }
}
