namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> CollectProcessGarbage(ProcessContext requester, Pid target, ProcessGarbageCollectionOptions options)
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        bool major = options.Type is Atom { Name: ProcessGarbageCollectionNames.Major };
        if (!options.Async && target.Equals(requester.Self))
        {
            // The internal owned profile has real registered guest roots at
            // this synchronous self safe point. Retire unreachable guest cells
            // before CLR collection; otherwise their slabs keep dead graphs alive.
            // Remote/asynchronous/off-actor/default paths are intentionally unchanged.
            if (OwnedTermsPilot506 && RuntimeActorExecutionTurn.IsOwnedBy(requester))
                requester.CollectOwnedGeneration508(major);
            requester.CollectManagedGarbage(major, CollectManagedHeap);

            return Term.A(ErlangBooleanAtoms.True);
        }
        Term requestId = options.Async ? options.RequestId : MakeReference();
        var request = new ProcessGarbageCollectionRequest(requester, requestId, major);
        lock (gate)
        {
            if (!processes.TryGetValue(target, out var process))
                ReplyGarbageCollection(request, false);
            else
            {
                process.PendingGarbageCollections.Add(request);
                process.Mailbox.SendPending(
                    Nil.Value,
                    () =>
                {
                    lock (gate)
                    {
                        if (process.PendingGarbageCollections.Remove(request))
                        {
                            process.CollectManagedGarbage(request.Major, CollectManagedHeap);
                            ReplyGarbageCollection(request, true, process.Self);
                        }
                    }

                    return null;
                }
                );
            }
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

    private void ReplyGarbageCollection(ProcessGarbageCollectionRequest request, bool result, Term? from = null) => SendSystem(
        request.Requester.Self,
        Term.Tuple(
            Term.A(ProcessGarbageCollectionNames.ReplyTag),
            request.Id,
            Term.A(result ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
        ),
        from
    );
}
