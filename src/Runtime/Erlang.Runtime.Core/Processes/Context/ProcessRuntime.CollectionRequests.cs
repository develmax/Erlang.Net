namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void AdmitSignals(ProcessContext owner)
    {
        try
        {
            AdmitSignalsCore(owner);
        }
        finally
        {
            DrainCollectionRequests(owner);
        }
    }

    private void ClaimCollectionRequest(ProcessContext target, ProcessGarbageCollectionRequest request)
    {
        lock (gate)
        {
            if (request.State != ProcessCollectionRequestState.Pending || target.Terminated
                || !target.PendingGarbageCollections.Contains(request))
                return;
            request.State = ProcessCollectionRequestState.Ready;
            target.ReadyGarbageCollections.AddLast(request.QueueNode);
        }
    }

    private void DrainCollectionRequests(ProcessContext target)
    {
        // AdmitSignals can also be called by locked Host readers and dirty
        // signal handlers. Those may claim metadata, never collect guest roots.
        if (!RuntimeActorExecutionTurn.IsOwnedBy(target) || System.Threading.Monitor.IsEntered(gate)
            || target.Mailbox.DeferredGateHeld)
            return;
        lock (gate)
        {
            if (target.DrainingGarbageCollections || target.Terminated || IsHaltRequested)
                return;
            target.DrainingGarbageCollections = true;
        }
        try
        {
            while (true)
            {
                ProcessGarbageCollectionRequest request;
                lock (gate)
                {
                    if (target.Terminated || IsHaltRequested || target.ReadyGarbageCollections.First is not { } node)
                        return;
                    target.ReadyGarbageCollections.RemoveFirst();
                    request = node.Value;
                    if (request.State != ProcessCollectionRequestState.Ready)
                        continue;
                    request.State = ProcessCollectionRequestState.Running;
                }
                try
                {
                    CollectAtProcessSafePoint(target, request.Major);
                }
                catch (Exception collectionFailure)
                {
                    try
                    {
                        CompleteCollectionRequest(request, false, true);
                    }
                    catch (Exception replyFailure)
                    {
                        throw new AggregateException(collectionFailure, replyFailure);
                    }
                    throw;
                }
                CompleteCollectionRequest(request, true, true);
            }
        }
        finally
        {
            lock (gate)
                target.DrainingGarbageCollections = false;
        }
    }

    private void CollectAtProcessSafePoint(ProcessContext target, bool major)
    {
        if (!RuntimeActorExecutionTurn.IsOwnedBy(target) || System.Threading.Monitor.IsEntered(gate)
            || target.Mailbox.DeferredGateHeld)
            throw new InvalidOperationException(RuntimeActorControlNames428.Foreign);
        target.Cancellation.ThrowIfCancellationRequested();
        ThrowIfHalted();
        target.CollectManagedGarbage(
            major,
            selectedMajor =>
        {
            if (target.VmStorageBackingEnabled)
                target.CollectOwnedGeneration508(selectedMajor);
            else
                CollectManagedHeap(selectedMajor); // existing opaque Host backing only
        }
        );
    }

    private void CompleteCollectionRequest(ProcessGarbageCollectionRequest request, bool result, bool sendReply)
    {
        lock (gate)
        {
            if (request.State == ProcessCollectionRequestState.Completed)
                return;
            request.State = ProcessCollectionRequestState.Completed;
            if (request.Target is { } target)
                target.PendingGarbageCollections.Remove(request);
            if (request.QueueNode.List is { } queue)
                queue.Remove(request.QueueNode);
            try
            {
                if (sendReply)
                    ReplyGarbageCollection(request, result, request.Target?.Self);
            }
            finally
            {
                // Only an idempotent metadata retirement latch under R. Actual
                // graph cleanup is independently scheduled by the existing641 owner.
                request.Custody.Close641();
            }
        }
    }

    private void RetireCollectionRequests(ProcessContext target, bool sendReplies)
    {
        // Called under R by both normal termination and immediate VM halt.
        List<Exception>? failures = null;
        while (target.PendingGarbageCollections.Count != 0)
        {
            ProcessGarbageCollectionRequest? request = null;
            foreach (var pending in target.PendingGarbageCollections)
            {
                request = pending;
                break;
            }
            try
            {
                CompleteCollectionRequest(request!, false, sendReplies);
            }
            catch (Exception failure)
            {
                (failures ??= new()).Add(failure);
            }
        }
        target.ReadyGarbageCollections.Clear();
        if (failures is not null)
            throw new AggregateException(failures);
    }
}
