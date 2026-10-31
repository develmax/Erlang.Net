namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<bool> TrySendBackedLocalAsync(
        Term target,
        Term message,
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
            lock (gate)
            {
                // Tracing/system/aliases/deferred producers are separate followup
                // tranches, not silently claimed physically backed here.
                if (!ProcessTracing.CanPublishDirectMailboxBacking()
                    || sender?.SequentialTrace.Token is not null) return false;
                if (!ResolveBackedMailboxTarget(target, out selectedPid)) return false;
                if (!processes.TryGetValue(selectedPid, out recipient!))
                {
                    if (recordSavedCall) sender!.SavedCalls.Sent();

                    return true;
                }
                if (recipient.Mailbox.DeferredGateHeld || sender?.Mailbox.DeferredGateHeld == true) return false;
                mode = recipient.Mailbox.MessageQueuePlacementMode;
                policy = recipient.Mailbox.MessageQueuePolicyRevision;
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
                    if (!ProcessTracing.CanPublishDirectMailboxBacking()
                        || sender?.SequentialTrace.Token is not null) return false;
                    if (!ResolveBackedMailboxTarget(target, out var selectedNow)) return false;
                    if (!selectedNow.Equals(selectedPid)) continue;
                    if (!processes.TryGetValue(selectedNow, out var current) || !ReferenceEquals(current, recipient))
                    {
                        if (recordSavedCall) sender!.SavedCalls.Sent();

                        return true;
                    }
                    if (recipient.Mailbox.MessageQueuePlacementMode != mode
                        || recipient.Mailbox.MessageQueuePolicyRevision != policy) continue;
                    if (recordSavedCall) sender!.SavedCalls.Sent();
                    recipient.Mailbox.SendTracedSnapshot593(
                        prepared.Value,
                        null,
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
