namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> CollectProcessGarbage(ProcessContext requester, Pid target, ProcessGarbageCollectionOptions options)
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        bool major = options.Type is Atom { Name: ProcessGarbageCollectionNames.Major };
        if (!options.Async && target.Equals(requester.Self))
        {
            if (RuntimeActorExecutionTurn.IsOwnedBy(requester))
                CollectAtProcessSafePoint(requester, major);
            else
                requester.CollectManagedGarbage(major, CollectManagedHeap); // existing opaque Host path only

            return Term.A(ErlangBooleanAtoms.True);
        }
        if (System.Threading.Monitor.IsEntered(gate) || requester.Mailbox.DeferredGateHeld)
            throw new InvalidOperationException(RuntimeActorControlNames428.Foreign);
        Term requestId = options.Async ? options.RequestId : MakeReference();
        // Capture while caller inputs are genuinely live and before R/M. This
        // runtime-owned queued operation outlives caller frame/cancellation.
        var custody = RuntimeProcessPayload641.Prepare641(this, null, requestId);
        ProcessGarbageCollectionRequest request;
        try
        {
            request = new(requester, major, custody);
        }
        catch
        {
            custody.Close641();
            throw;
        }
        try
        {
            lock (gate)
            {
                if (!processes.TryGetValue(target, out var process))
                    CompleteCollectionRequest(request, false, true);
                else
                {
                    request.Target = process;
                    process.PendingGarbageCollections.Add(request);
                    request.State = ProcessCollectionRequestState.Pending;
                    process.Mailbox.SendPending(Nil.Value, () =>
                    {
                        ClaimCollectionRequest(process, request);

                        return null;
                    });
                }
            }
        }
        catch
        {
            CompleteCollectionRequest(request, false, false);
            throw;
        }
        if (options.Async)
            return Term.A(ProcessGarbageCollectionNames.AsyncResult);
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple &&
            tuple.Items[0] is Atom { Name: ProcessGarbageCollectionNames.ReplyTag } && tuple.Items[1].Equals(requestId) ? tuple : null);

        return reply!.Items[2];
    }

    private static void CollectManagedHeap(bool major)
    {
        GC.Collect(
            major ? GC.MaxGeneration : ProcessGarbageCollectionParameters.ManagedYoungGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: major
        );
    }

    private void ReplyGarbageCollection(ProcessGarbageCollectionRequest request, bool result, Term? from = null)
        => SendMonitorPayload641(
            request.Requester,
            Term.Tuple(
                Term.A(ProcessGarbageCollectionNames.ReplyTag),
                request.Id,
                Term.A(result ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
            ),
            null,
            request.Custody,
            from: from
        );
}
