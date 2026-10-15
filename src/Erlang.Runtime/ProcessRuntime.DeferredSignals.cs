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
        lock (gate)
        {
            if (!ReferenceEquals(owner.Runtime, this) || !processes.TryGetValue(owner.Self, out var found) ||
                !ReferenceEquals(found, owner) || owner.Terminated)
            {
                release();

                return;
            }
            owner.DeferredSignalDrain ??= new();
            owner.Mailbox.SendDeferred(
                prepare,
                release,
                from,
                traceToken
            );
        }
    }

    private void DrainDeferredProcessSignals(ProcessContext owner)
    {
        while (true)
        {
            MailboxDeferredPreparation? work;
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
                work = owner.Mailbox.ClaimDeferred();
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
            lock (gate)
                work.Complete(owner.Terminated ? null : ready);
        }
    }
}
