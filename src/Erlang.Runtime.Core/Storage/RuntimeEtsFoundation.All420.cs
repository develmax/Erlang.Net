namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private readonly LinkedList<RuntimeEtsFoundationTable>[] creationLanes420 = Enumerable.Range(0, runtime.ActorDispatchCapacity).Select(_ => new LinkedList<RuntimeEtsFoundationTable>()).ToArray();
    private readonly HashSet<RuntimeEtsAllRequest420> allRequests420 = new();

    internal ReferenceTerm CoreRequestAll420(ProcessContext context)
    {
        ReferenceTerm reference;
        lock (gate)
        {
            RequireActor(context);
            reference = runtime.MakeReference();
        }
        var request = new RuntimeEtsAllRequest420(this, context, reference);
        try
        {
            lock (gate)
            {
                RequireActor(context);
                allRequests420.EnsureCapacity(checked(allRequests420.Count + 1));
                allRequests420.Add(request);
                request.Registered = true;
            }
            request.Batch.Publish();

            return reference;
        }
        catch (Exception error)
        {
            if (request.Batch.State == RuntimeSchedulerAuxiliaryState420.Prepared) request.Batch.Abort(error);
            throw;
        }
    }

    internal void ReplyAllLane420(RuntimeEtsAllRequest420 request, int identity)
    {
        Term[] tables;
        lock (gate)
        {
            // Actual per-created-lane directory, not partitioned duplicate aggregate replies.
            tables = closed ? Array.Empty<Term>() : creationLanes420[identity - ProcessSchedulerNames.First]
             .Where(IsTableVisible419).Select(table => table.Named ? (Term)table.Name : table.Handle).ToArray();
            if (!runtime.EtsAllRecipient420(request.Context)) return;
        }
        Term message = Term.Tuple(request.Reference, Term.List(tables));
        var admission = request.Context.Mailbox.PrepareEtsAll420(request.Context, message);
        lock (gate)
        {
            if (!runtime.EtsAllRecipient420(request.Context)) return;
            admission.Validate(request.Context.Mailbox);
            admission.Track(request.Context);
            admission.Commit(request.Context.Mailbox);
        }
        admission.Notify();
    }

    internal void AllRequestEnded420(RuntimeEtsAllRequest420 request, IEnumerable<Exception>? failures)
    {
        lock (gate)
        {
            try
            {
                if (request.Registered && failures is not null) retirementFailures.AddRange(failures);
            }
            finally
            {
                allRequests420.Remove(request);
            }
        }
    }
}
