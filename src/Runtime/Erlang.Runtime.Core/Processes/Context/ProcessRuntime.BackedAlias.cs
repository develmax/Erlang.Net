namespace Erlang;

public sealed partial class ProcessRuntime
{
    private async ValueTask<bool> TrySendBackedAliasAsync(
        ReferenceTerm reference,
        Term message,
        bool priority,
        ProcessContext? sender
    )
    {
        if (System.Threading.Monitor.IsEntered(gate) || !MailboxValueEscape.CanMaterialize(this, message)) return false;
        while (true)
        {
            ProcessContext? recipient;
            ProcessAliasEntry? expectedAlias;
            bool sequenceEvent;
            SequenceTraceSendPreview preview = default;
            ProcessTermBorrow506? borrow = null;
            ProcessMessageQueueDataMode411 mode = default;
            long policy = 0;
            lock (gate)
            {
                Pid? traceOwner = ResolveAliasOwner(reference);
                sequenceEvent = sender is not null && traceOwner is not null && processes.ContainsKey(traceOwner);
                aliases.TryGetValue(reference, out expectedAlias);
                recipient = expectedAlias is not null && processes.TryGetValue(expectedAlias.Owner, out var target) ? target : null;
                if (!ProcessTracing.CanPublishDirectMailboxBacking() || !ProcessTracing.SupportsMessageSnapshots593()) return false;
                if (recipient?.Mailbox.DeferredGateHeld == true || sender?.Mailbox.DeferredGateHeld == true) return false;
                if (recipient is not null)
                {
                    if (recipient.Mailbox.IsClosedForBacking) return false;
                    mode = recipient.Mailbox.MessageQueuePlacementMode;
                    policy = recipient.Mailbox.MessageQueuePolicyRevision;
                }
                if (sequenceEvent)
                {
                    preview = SequenceTracing.PreviewSent(sender!);
                    borrow = sender!.SequentialTrace.BorrowPhysical();
                }
            }
            await using PreparedOrdinarySequenceSend? sequence = sequenceEvent
                ? await PreparedOrdinarySequenceSend.PrepareCapturedAsync(
                    sender!,
                    reference,
                    message,
                    preview,
                    borrow
                ).ConfigureAwait(false)
                : null;
            PreparedMailboxMessage? prepared = null;
            try
            {
                if (recipient is not null)
                {
                    try
                    {
                        prepared = sequence?.Token is { } token
                            ? await mailboxRoots593.PrepareMailboxMessageAsync(
                                this,
                                recipient,
                                mode,
                                message,
                                token
                            ).ConfigureAwait(false)
                            : await mailboxRoots593.PrepareMailboxMessageAsync(
                                this,
                                recipient,
                                mode,
                                message
                            ).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        lock (gate)
                        {
                            if (!aliases.TryGetValue(reference, out var currentAlias) || !ReferenceEquals(currentAlias, expectedAlias)
                                || !processes.TryGetValue(currentAlias.Owner, out var currentOwner)
                                || !ReferenceEquals(currentOwner, recipient)) continue;
                            if (recipient.Mailbox.IsClosedForBacking) return false;
                        }
                        throw;
                    }
                }
                using var publication = prepared is null ? null : MailboxPublicationScope593.Forward(this, prepared.Graph);
                lock (gate)
                {
                    Pid? traceOwner = ResolveAliasOwner(reference);
                    bool currentSequenceEvent = sender is not null && traceOwner is not null && processes.ContainsKey(traceOwner);
                    if (sequenceEvent != currentSequenceEvent || sequence is not null && !sequence.IsCurrent()) continue;
                    aliases.TryGetValue(reference, out var currentAlias);
                    if (!ReferenceEquals(currentAlias, expectedAlias)) continue;
                    ProcessContext? currentOwner = currentAlias is not null && processes.TryGetValue(currentAlias.Owner, out var actual) ? actual : null;
                    if (!ProcessTracing.CanPublishDirectMailboxBacking() || !ProcessTracing.SupportsMessageSnapshots593()) return false;
                    if (!ReferenceEquals(currentOwner, recipient)) continue;
                    if (recipient is not null && (recipient.Mailbox.MessageQueuePlacementMode != mode
                        || recipient.Mailbox.MessageQueuePolicyRevision != policy)) continue;
                    if (sender is not null) ProcessTracing.Sent(
                        sender,
                        reference,
                        message,
                        true
                    );
                    sequence?.Commit();
                    if (recipient is not null)
                    {
                        // Keep original pending admission: reply alias removal,
                        // monitor detachment and priority decision occur ONLY
                        // when this exact queued entry reaches admission.
                        recipient.Mailbox.SendPendingSnapshot593(
                            prepared!.Value,
                            new PendingAliasAdmission817(
                                this,
                                recipient,
                                reference,
                                priority
                            ).Callback,
                            prepared.Token,
                            from: sender?.Self,
                            publication593: publication
                        );
                    }

                    return true;
                }
            }
            finally
            {
                if (prepared is not null) await prepared.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
