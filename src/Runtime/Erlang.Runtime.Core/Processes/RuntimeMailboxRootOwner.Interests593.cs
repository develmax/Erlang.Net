namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
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
