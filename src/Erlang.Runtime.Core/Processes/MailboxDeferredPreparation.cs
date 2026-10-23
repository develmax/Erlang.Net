namespace Erlang;

internal sealed record MailboxDeferredPreparation(
    Func<DeferredMailboxAdmission?> Prepare,
    Action<DeferredMailboxAdmission?> Complete
);
