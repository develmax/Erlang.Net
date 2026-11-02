namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ValueTask<bool> TrySendBackedLocalAsync(
        Term target,
        Term message,
        ProcessContext? sender = null,
        bool recordSavedCall = false,
        bool priority = false,
        Term? from = null,
        bool preserve = false
    ) => TrySendBackedLocalWithLiteral824Async(
        target,
        message,
        null,
        sender,
        recordSavedCall,
        priority,
        from,
        preserve
    );

    internal async ValueTask<bool> TrySendBackedLocalWithLiteral824Async(
        Term target,
        Term message,
        ModuleLiteralRoots801? carriedLiteral801,
        ProcessContext? sender = null,
        bool recordSavedCall = false,
        bool priority = false,
        Term? from = null,
        bool preserve = false
    )
    {
        // A recursive legacy/system caller holding R must use its existing
        // path until it receives its own outer-preparation tranche.
        if (System.Threading.Monitor.IsEntered(gate) || !MailboxValueEscape.CanMaterialize(this, message)) return false;
        while (true)
        {
            ProcessContext recipient;
            Pid selectedPid;
            ProcessMessageQueueDataMode411 mode;
            long policy;
            SequenceTraceSendPreview preview = default;
            ProcessTermBorrow506? borrow = null;
            lock (gate)
            {
                // Tracing/system/aliases/deferred producers are separate followup
                // tranches, not silently claimed physically backed here.
                if (!ResolveBackedMailboxTarget(target, out selectedPid)) return false;
                bool recipientAlive = processes.TryGetValue(selectedPid, out recipient!);
                if (!ProcessTracing.CanPublishDirectMailboxBacking() || !ProcessTracing.SupportsMessageSnapshots593()) return false;
                if (!recipientAlive)
                {
                    if (recordSavedCall) sender!.SavedCalls.Sent();

                    return true;
                }
                if (recipient.Mailbox.DeferredGateHeld || sender?.Mailbox.DeferredGateHeld == true) return false;
                // Token-bearing self delivery still needs an exact same-heap
                // Value retention plus independent physical token envelope.
                if (sender is not null && sender.Self.Equals(selectedPid) && sender.SequentialTrace.Token is not null) return false;
                mode = recipient.Mailbox.MessageQueuePlacementMode;
                policy = recipient.Mailbox.MessageQueuePolicyRevision;
                if (sender is not null)
                {
                    preview = SequenceTracing.PreviewSent(sender);
                    borrow = sender.SequentialTrace.BorrowPhysical();
                }
            }
            await using PreparedOrdinarySequenceSend? sequence = sender is null ? null
                : await PreparedOrdinarySequenceSend.PrepareCapturedAsync(
                    sender,
                    selectedPid,
                    message,
                    preview,
                    borrow
                ).ConfigureAwait(false);
            PreparedMailboxMessage prepared;
            try
            {
                prepared = sequence?.Token is { } token
                    ? await mailboxRoots593.PrepareMailboxMessageAsync(
                        this,
                        recipient,
                        mode,
                        message,
                        token,
                        carriedLiteral801
                    ).ConfigureAwait(false)
                    : await mailboxRoots593.PrepareMailboxMessageWithLiteral824Async(
                        this,
                        recipient,
                        mode,
                        message,
                        carriedLiteral801
                    ).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Admission/target close after preflight is a dropped local send;
                // preparation already independently settled its exact cleanup.
                lock (gate)
                {
                    if (!ResolveBackedMailboxTarget(target, out var selectedNow)) return false;
                    if (!selectedNow.Equals(selectedPid)) continue;
                    if (!processes.TryGetValue(selectedNow, out var current) || !ReferenceEquals(current, recipient))
                    {
                        if (recordSavedCall) sender!.SavedCalls.Sent();

                        return true;
                    }
                }
                throw;
            }
            await using (prepared.ConfigureAwait(false))
            {
                using var publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                lock (gate)
                {
                    if (sequence is not null && !sequence.IsCurrent()) continue;
                    if (!ResolveBackedMailboxTarget(target, out var selectedNow)) return false;
                    if (!selectedNow.Equals(selectedPid)) continue;
                    bool currentAlive = processes.TryGetValue(selectedNow, out var current);
                    if (!ProcessTracing.CanPublishDirectMailboxBacking() || !ProcessTracing.SupportsMessageSnapshots593()) return false;
                    if (!currentAlive || !ReferenceEquals(current, recipient))
                    {
                        if (recordSavedCall) sender!.SavedCalls.Sent();

                        return true;
                    }
                    if (recipient.Mailbox.MessageQueuePlacementMode != mode
                        || recipient.Mailbox.MessageQueuePolicyRevision != policy) continue;
                    if (recordSavedCall) sender!.SavedCalls.Sent();
                    sequence?.Commit();
                    recipient.Mailbox.SendTracedSnapshot593(
                        prepared.Value,
                        prepared.Token,
                        preserve: preserve,
                        priority: priority,
                        from: sender?.Self ?? from,
                        publication593: publication
                    );

                    return true;
                }
            }
        }
    }

    // Called only under R; final publication repeats the SAME name resolution.
    private bool ResolveBackedMailboxTarget(Term target, out Pid pid)
    {
        if (target is Pid direct && direct.Node == Node)
        {
            pid = direct;

            return true;
        }
        if (target is Atom name && names.TryGetValue(name.Name, out var registered)
            && registered is Pid named && named.Node == Node)
        {
            pid = named;

            return true;
        }
        pid = InitializationPid;

        return false;
    }
}
