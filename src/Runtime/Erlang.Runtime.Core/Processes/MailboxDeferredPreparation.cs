namespace Erlang;

internal sealed record MailboxDeferredPreparation(
    Func<DeferredMailboxAdmission?> Prepare,
    Action<DeferredMailboxAdmission?> Complete,
    Action? Release = null,
    bool AllowBacking = false
);
