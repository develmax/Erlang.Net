namespace Erlang;

public sealed partial class Mailbox
{
    // Preallocated lists reuse the actual Entry nodes. Close neither allocates
    // a callback array nor invokes arbitrary deferred release under M/R.
    private readonly LinkedList<Entry> retiredEntries593 = new();
    private readonly LinkedList<Entry> failedRetirements593 = new();
    private Task? retirement593;
    private bool entriesCaptured606;

    // This is the only operation required under runtime R. Physical queue
    // capture and release callbacks must not acquire mailbox M from that turn.
    internal void StopAdmissionForExit606() => Volatile.Write(ref closed593, true);

    internal Task DrainAfterQueueObservations606(Task observations)
    {
        StopAdmissionForExit606();
        if (ExecutionContext.IsFlowSuppressed())
            return Task.Run(() => DrainAfterQueueObservationsCore606(observations));

        using (ExecutionContext.SuppressFlow())
            return Task.Run(() => DrainAfterQueueObservationsCore606(observations));
    }

    private async Task DrainAfterQueueObservationsCore606(Task observations)
    {
        Exception? observationFailure = null, captureFailure = null, drainFailure = null;
        try
        {
            await observations.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            observationFailure = error;
        }
        // A faulted predecessor cannot suppress independent physical cleanup.
        // Task.Run suppressed actor/owned-invocation context and owns no R/M.
        try
        {
            CloseForExit593();
        }
        catch (Exception error)
        {
            captureFailure = error;
        }
        try
        {
            await DrainClosedPayload593().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            drainFailure = error;
        }
        if (observationFailure is not null || captureFailure is not null || drainFailure is not null)
            throw new AggregateException(new[] { observationFailure, captureFailure, drainFailure }.OfType<Exception>());
    }

    internal void CloseForExit593()
    {
        StopAdmissionForExit606();
        lock (gate)
        {
            if (entriesCaptured606)
                return;
            PayloadRetirements593.Close();
            while (messages.First is { } node)
            {
                messages.RemoveFirst();
                node.Value.Custody593?.Dispose(); // Atomic release/schedule only.
                if (node.Value.DeferredRelease is not null)
                    retiredEntries593.AddLast(node);
            }

            ClearReceiveMarkersForHalt();
            ClearPendingSignals428();
            beamSave = null;
            pending = deferredPending = admittedCount = 0;
            lastPriority = null;
            mutationRevision++;
            precedenceRevision++;
            entriesCaptured606 = true;
            // No new signal allocation is needed: closed mailboxes never wait
            // for a subsequent pulse. Existing continuations run asynchronously.
            changed.TrySetResult();
        }
    }

    internal Task DrainClosedPayload593()
    {
        lock (gate)
        {
            if (retirement593 is not null)
                return retirement593;

            if (retiredEntries593.Count == 0 && !PayloadRetirements593.NeedsDrain593)
                return Task.CompletedTask;

            return retirement593 = DrainClosedPayloadCore593();
        }
    }

    private async Task DrainClosedPayloadCore593()
    {
        await Task.Yield();
        while (true)
        {
            LinkedListNode<Entry>? node;
            lock (gate)
            {
                node = retiredEntries593.First;
                if (node is null)
                    break;
                retiredEntries593.RemoveFirst();
            }

            try
            {
                node.Value.DeferredRelease?.Invoke();
            }
            catch (Exception error)
            {
                // Record directly on the existing node; do not allocate an
                // error list that could prevent later independent cleanup.
                node.Value = node.Value with { RetirementFailure593 = error };
                lock (gate) failedRetirements593.AddLast(node);
            }
        }

        Exception? rootsFailure = null;
        try
        {
            await PayloadRetirements593.DrainAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            rootsFailure = error;
        }
        Exception[] failures;
        lock (gate)
            failures = failedRetirements593.Select(entry => entry.RetirementFailure593)
                .Append(rootsFailure).OfType<Exception>().ToArray();

        if (failures.Length != 0)
            throw new AggregateException(failures);
    }
}
