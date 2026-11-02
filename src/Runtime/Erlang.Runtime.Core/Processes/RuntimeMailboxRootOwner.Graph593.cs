namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Only this entry performs actual producer/header validation and typed
    // graph retention. Caller must execute it before runtime/mailbox monitors.
    internal MailboxGraphSnapshot593? PrepareGraph(ProcessRuntime runtime, Term completeGraph)
        => PrepareGraphWithLiteral814(runtime, completeGraph, null);

    internal MailboxGraphSnapshot593? PrepareGraphWithLiteral814(
        ProcessRuntime runtime, Term completeGraph, ModuleLiteralRoots801? carriedLiteral801)
    {
        MailboxPayloadLease593? roots = CaptureWithLiteral814(runtime, completeGraph, carriedLiteral801);
        if (roots is null)
            return null;

        try
        {
            return new MailboxGraphSnapshot593(runtime, roots);
        }
        catch (Exception original)
        {
            roots.ReleaseOwner();
            try
            {
                MailboxRootSettlement593.Settle(roots);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    // Trusted runtime publication only: graph must originate from this actual
    // runtime and cover every native-bearing leaf of the new queue payload.
    // The recipient gets its OWN work item, never an operation reference to
    // another recipient. Thus local exit does not wait for a trace recipient.
    internal MailboxForkResult593 ForkForRecipient(
        ProcessRuntime runtime, MailboxGraphSnapshot593? graph, MailboxRetirementTracker593 recipient
    ) => ForkCore593(
        runtime,
        graph,
        recipient,
        null
    );

    internal MailboxForkResult593 ForkForPublication(
        MailboxPublicationScope593 publication, MailboxRetirementTracker593 recipient
    ) => ForkCore593(
        publication.Runtime,
        publication.Graph,
        recipient,
        publication
    );

    private MailboxForkResult593 ForkCore593(
        ProcessRuntime runtime,
        MailboxGraphSnapshot593? graph,
        MailboxRetirementTracker593 recipient,
        MailboxPublicationScope593? publication
    )
    {
        if (graph is null)
            return default;

        if (!ReferenceEquals(graph.Runtime, runtime))
            return new(null, null, new NotSupportedException(ProcessTerm506Names.ForeignRoot));

        // Any failure before registering work has retained no extra graph
        // reference. After registration every unwind returns this specific
        // work token for outside-monitor progress, including scheduling failure.
        var work = new MailboxPayloadLease593(this);
        bool registered = false;
        try
        {
            publication?.Track(work);
            lock (gate)
            {
                // Publication/Entry callers still hold an actual parent graph
                // interest while forking; only that anchored derivation may
                // continue after fresh admission stops.
                ObjectDisposedException.ThrowIf(closed && !IsLiveGraphAnchor593(graph), this);
                active.AddLast(work.Link);
                registered = true;
                work.MarkRegistered593();
            }

            IDisposable retained = graph.Retain();
            work.AttachSnapshot(retained, graph);
            // Only the exact recipient receives the actual physical frame.
            // Snapshot/trace forks retain portable observation/native custody,
            // never another process's on_heap queue frame.
            if (graph.RootWork593.TakeRecipientPreparation(recipient) is { } fragment)
                work.AttachRecipientFragment(fragment, graph.ObservationValue);
            work.BindRecipient(recipient);

            return new(work, null, null);
        }
        catch (Exception original)
        {
            if (registered)
                work.ReleaseOwner();

            // This is a value outcome, not an exception that drops cleanup
            // authority while the runtime publication monitor is still held.
            return new(null, registered ? work : null, original);
        }
    }
}
