namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void QueueDeferredProcessSignal(
        ProcessContext owner,
        Func<DeferredMailboxAdmission?> prepare,
        Action release,
        Term? from = null,
        Term? traceToken = null
    )
    {
        bool accepted = false;
        lock (gate)
        {
            if (ReferenceEquals(owner.Runtime, this) && processes.TryGetValue(owner.Self, out var found)
                && ReferenceEquals(found, owner) && !owner.Terminated)
            {
                owner.DeferredSignalDrain ??= new();
                accepted = owner.Mailbox.TrySendDeferred606(
                    prepare,
                    release,
                    from,
                    traceToken
                );
            }
        }
        if (!accepted) release(); // No runtime R or mailbox M during rejected cleanup.
    }

    private void DrainDeferredProcessSignals(ProcessContext owner)
    {
        while (true)
        {
            MailboxDeferredPreparation? work;
            MailboxPendingBackingClaim? pendingClaim;
            lock (gate)
            {
                if (owner.Terminated)
                {
                    owner.Mailbox.DiscardPending();

                    return;
                }
                owner.Mailbox.AdmitPending();
                if (owner.Terminated)
                {
                    owner.Mailbox.DiscardPending();

                    return;
                }
                pendingClaim = owner.Mailbox.ClaimPendingBacking();
                work = pendingClaim is null ? owner.Mailbox.ClaimDeferred() : null;
            }
            if (pendingClaim is not null)
            {
                PrepareAndAdmitPendingMailboxBacking(owner, pendingClaim, null);
                continue;
            }
            if (work is null)
                return;
            DeferredMailboxAdmission? ready;
            try
            {
                // Neither global runtime nor mailbox gate is held here.
                ready = work.Prepare();
            }
            catch
            {
                lock (gate)
                    work.Complete(null);
                throw;
            }
            try
            {
                lock (gate)
                    work.Complete(owner.Terminated ? null : ready);
            }
            finally
            {
                ready?.Payload641?.Close641();
            }
        }
    }
}
