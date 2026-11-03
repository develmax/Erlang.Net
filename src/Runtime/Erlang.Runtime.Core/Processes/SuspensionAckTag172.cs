namespace Erlang;

// Deferred ACK metadata plus an actual runtime-owned graph. The portable proof
// is not native lifetime; the graph owns every reserved/admitted typed root.
internal sealed class SuspensionAckTag172 : IDisposable, IAsyncDisposable
{
    private readonly PortableControlTerm826 tag;
    private readonly MailboxGraphSnapshot593? graph;
    private readonly NativeResourceRootLease524[] reservations;

    internal SuspensionAckTag172(PortableControlTerm826 tag, MailboxGraphSnapshot593? graph,
        NativeResourceRootLease524[] reservations)
    {
        this.tag = tag;
        this.graph = graph;
        this.reservations = reservations;
    }

    internal void Admit172()
    {
        foreach (NativeResourceRootLease524 reservation in reservations)
            reservation.AdmitSuspensionAck172();
    }

    internal void Publish172(ProcessContext recipient, Atom status, Pid? from)
    {
        var publication = MailboxPublicationScope593.Forward(recipient.Runtime, graph);
        try
        {
            recipient.Mailbox.SendTracedSnapshot593(
                Term.Tuple(tag.Value, status),
                null,
                preserve: true,
                from: from,
                publication593: publication
            );
        }
        finally
        {
            publication.ReleaseForwardDeferred172();
        }
    }

    // Under R only schedule; outside-R preparation failure/finally can join.
    public void Dispose() => graph?.Dispose();

    public ValueTask DisposeAsync() => graph?.DisposeAsync() ?? ValueTask.CompletedTask;
}
