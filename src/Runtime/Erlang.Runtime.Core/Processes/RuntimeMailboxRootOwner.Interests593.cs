namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Derived publication only. Its typed group already owns actual interests
    // in every supplied parent. Fresh Capture and generic snapshot admission
    // retain their independent closed checks.
    internal MailboxGraphSnapshot593 PrepareProcessPayloadInterestGraph641(
        ProcessRuntime runtime,
        RuntimeProcessPayloadGroup641 group
    )
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!ReferenceEquals(runtime.MailboxRoots593, this))
            throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);
        var work = new MailboxPayloadLease593(this);
        var graph = new MailboxGraphSnapshot593(runtime, work);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(drained.Task.IsCompleted || !group.HasLiveInterests593(this), this);
            // The parent remains in active until after this child is added.
            // Thus StopAdmission cannot complete drained across the handoff.
            active.AddLast(work.Link);
            work.Attach(group);
            work.MarkRegistered593();
        }

        return graph;
    }

    // Caller holds gate. An actual active root node is the authority; matching
    // runtime names, an IDisposable or an already-retired graph is insufficient.
    internal bool IsLiveGraphAnchor593(MailboxGraphSnapshot593 graph)
        => !drained.Task.IsCompleted
            && ReferenceEquals(graph.Runtime.MailboxRoots593, this)
            && ReferenceEquals(graph.RootWork593.Owner593, this)
            && graph.HasGraphReferences593
            && ReferenceEquals(graph.RootWork593.Link.List, active);

    // Callback-free group handoff, permitted while R/M is held. The caller's
    // group already owns real graph interests acquired from protected Entries.
    // On ANY exception ownership remains with caller: every throwing allocation
    // and admission check occurs before the nonthrowing registration/handoff.
    internal MailboxGraphSnapshot593 PrepareInterestGraph593(ProcessRuntime runtime, IDisposable group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!ReferenceEquals(runtime.MailboxRoots593, this))
            throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);

        var work = new MailboxPayloadLease593(this);
        var graph = new MailboxGraphSnapshot593(runtime, work);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.Attach(group);
            work.MarkRegistered593();
        }

        return graph;
    }
}
