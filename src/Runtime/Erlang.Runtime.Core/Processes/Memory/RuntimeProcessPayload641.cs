namespace Erlang;

internal sealed class RuntimeProcessPayload641 : IAsyncDisposable
{
    private readonly RuntimeProcessPayloadWork641 work;
    private RuntimePendingPayload628? prepared;
    private MailboxGraphSnapshot593? graph;
    private IMailboxRootInterest593? borrowed;
    private IDisposable? rollback;
    private MailboxPublicationScope593? publication;
    private Term? value;
    private bool ownsCompositeGraph;
    internal ProcessRuntime Runtime641 => work.Owner641.Runtime641;
    internal Term Value641 => value ?? throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
    internal MailboxGraphSnapshot593? Graph641 => graph;
    internal MailboxPublicationScope593 Publication641 => publication
        ?? throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
    internal Task Completion641 => work.Completion641;

    private RuntimeProcessPayload641(RuntimeProcessPayloadWork641 work) => this.work = work;

    private static RuntimeProcessPayload641 New641(RuntimeProcessPayloadOwner641 owner, ProcessContext? context, bool derived)
    {
        RuntimeProcessPayloadWork641 work = owner.Reserve641(context, derived);
        try
        {
            var result = new RuntimeProcessPayload641(work);
            work.Attach641(result); // BEFORE any physical/graph capture

            return result;
        }
        catch
        {
            work.End641();
            throw;
        }
    }

    internal static RuntimeProcessPayload641 Prepare641(ProcessRuntime runtime, ProcessContext? context, Term completeSourceGraph)
    {
        RuntimeProcessPayload641 result = Reserve641(runtime, context);
        try
        {
            result.Capture641(completeSourceGraph);

            return result;
        }
        catch
        {
            result.Close641();
            throw;
        }
    }

    internal static RuntimeProcessPayload641 Reserve641(ProcessRuntime runtime, ProcessContext? context)
        => New641(runtime.ProcessPayloads641, context, false);

    internal void Capture641(Term completeSourceGraph)
    {
        if (value is not null || prepared is not null)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        prepared = RuntimePendingPayload628.Prepare(Runtime641, Term.Tuple(completeSourceGraph));
        value = prepared.Value.Items[0];
        graph = prepared.Graph;
    }

    internal RuntimeProcessPayload641 Borrow641(ProcessContext? context)
    {
        RuntimeProcessPayload641 result = New641(work.Owner641, context, true);
        try
        {
            result.value = Value641;
            result.graph = Graph641;
            result.borrowed = (IMailboxRootInterest593?)graph?.Retain();

            return result;
        }
        catch
        {
            result.Close641();
            throw;
        }
    }

    internal static RuntimeProcessPayload641 CombinePublication641(
        ProcessRuntime runtime,
        ProcessContext? context,
        Term completeStableMessage,
        RuntimeProcessPayload641? first,
        RuntimeProcessPayload641? second
    )
    {
        if (first is not null && !ReferenceEquals(first.Runtime641, runtime)
            || second is not null && !ReferenceEquals(second.Runtime641, runtime))
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Foreign);
        RuntimeProcessPayloadOwner641 owner = first?.work.Owner641 ?? second?.work.Owner641 ?? runtime.ProcessPayloads641;
        RuntimeProcessPayload641 result = New641(owner, context, true);
        try
        {
            if (first?.Graph641 is not null || second?.Graph641 is not null)
            {
                var group = new RuntimeProcessPayloadGroup641();
                result.rollback = group; // BEFORE partial interest acquisition
                group.Capture641(first?.Graph641, second?.Graph641);
                result.graph = runtime.MailboxRoots593.PrepareInterestGraph593(runtime, group);
                result.ownsCompositeGraph = true;
                result.rollback = null; // graph now owns the actual group
            }
            result.value = completeStableMessage;
            result.publication = MailboxPublicationScope593.Forward(runtime, result.graph);

            return result;
        }
        catch
        {
            result.Close641();
            throw;
        }
    }

    internal void Close641() => work.End641(); // Metadata only, allowed R/M.

    internal void CloseAfter641(Task actualOperation) => work.EndAfter641(actualOperation);

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        Exception? scopeFailure = null, rootFailure = null, fallbackFailure = null;
        try
        {
            Interlocked.Exchange(ref publication, null)?.Dispose();
        }
        catch (Exception error)
        {
            scopeFailure = error;
        }
        try
        {
            if (prepared is not null) await prepared.DisposeAsync().ConfigureAwait(false);
            else if (borrowed is not null)
                await MailboxRootSettlement593.SettleAsync(Interlocked.Exchange(ref borrowed, null)?.ReleaseLast()).ConfigureAwait(false);
            else if (ownsCompositeGraph && graph is not null) await graph.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            rootFailure = error;
        }
        try
        {
            Interlocked.Exchange(ref rollback, null)?.Dispose();
        }
        catch (Exception error)
        {
            fallbackFailure = error;
        }
        Exception[] failures = new[] { scopeFailure, rootFailure, fallbackFailure }.OfType<Exception>().ToArray();
        if (failures.Length != 0) throw new AggregateException(failures);
    }
}
