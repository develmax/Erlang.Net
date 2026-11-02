namespace Erlang;

public sealed partial class ProcessRuntime
{
    private async ValueTask<bool> TryPublishBackedLocalExitAsync(ProcessContext sender, Pid target, Term reason)
    {
        // No physical preparation can run under legacy R/M callbacks. Such
        // callers retain their existing signal path until separately ported.
        if (System.Threading.Monitor.IsEntered(gate) || sender.Mailbox.DeferredGateHeld)
            return false;
        Term message = Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender.Self, reason);
        if (!MailboxValueEscape.CanMaterialize(this, message))
            return false;
        while (true)
        {
            ProcessContext recipient;
            ProcessMessageQueueDataMode411 mode;
            long revision;
            lock (gate)
            {
                if (disposed || IsHaltRequested || !ProcessTracing.CanPublishDirectMailboxBacking())
                    return false;
                if (!processes.TryGetValue(target, out recipient!) || recipient.Terminated)
                    return true;
                if (!recipient.TrapExits || reason.Equals(Term.A(ProcessExitSignals.Kill))
                    || recipient.Mailbox.DeferredGateHeld)
                    return false;
                mode = recipient.Mailbox.MessageQueuePlacementMode;
                revision = recipient.Mailbox.MessageQueuePolicyRevision;
            }
            PreparedMailboxMessage prepared;
            try
            {
                prepared = await mailboxRoots593.PrepareMailboxMessageAsync(
                    this,
                    recipient,
                    mode,
                    message
                )
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Preparation has independently settled its actual rollback.
                lock (gate)
                {
                    if (!processes.TryGetValue(target, out var current)
                        || !ReferenceEquals(current, recipient) || recipient.Terminated)
                        return true;
                    if (disposed || IsHaltRequested || recipient.Mailbox.IsClosedForBacking)
                        return false;
                }
                throw;
            }
            Exception? primary = null;
            try
            {
                using var publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                lock (gate)
                {
                    if (!processes.TryGetValue(target, out var current)
                        || !ReferenceEquals(current, recipient) || recipient.Terminated)
                        return true;
                    if (disposed || IsHaltRequested || !ProcessTracing.CanPublishDirectMailboxBacking()
                        || !recipient.TrapExits || reason.Equals(Term.A(ProcessExitSignals.Kill)))
                        return false;
                    if (recipient.Mailbox.MessageQueuePlacementMode != mode
                        || recipient.Mailbox.MessageQueuePolicyRevision != revision)
                        continue;
                    // Same immediate trapping Signal branch and transport
                    // metadata. No new lazy control/decode position is created.
                    recipient.Mailbox.SendTracedSnapshot593(
                        prepared.Value,
                        null,
                        from: sender.Self,
                        publication593: publication
                    );

                    return true;
                }
            }
            catch (Exception error)
            {
                primary = error;
                throw;
            }
            finally
            {
                try
                {
                    await prepared.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup) when (primary is not null && !ReferenceEquals(primary, cleanup))
                {
                    throw new AggregateException(primary, cleanup);
                }
            }
        }
    }
}
