namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool PublishReadonlyTraceFragment817(
        ProcessContext tracer,
        MailboxGraphSnapshot593 graph,
        Term exactNativeValue,
        OrdinaryBinaryLease[] leases
    )
    {
        if (!System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(tracer.Runtime, this)
            || !ReferenceEquals(graph.Runtime, this)) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        if (IsHaltRequested || tracer.Terminated || tracer.Mailbox.IsClosedForBacking
            || !processes.TryGetValue(tracer.Self, out ProcessContext? current) || !ReferenceEquals(current, tracer)) return false;
        foreach (OrdinaryBinaryLease lease in leases)
            lease.Owner.RetainReadonlyReserved817(lease);
        using var publication = MailboxPublicationScope593.Forward(this, graph);
        // PIN erl_tracer_nif enif_send(env, pid, NULL, msg) uses a detached
        // fragment for BOTH receiver queue modes, with am_undefined token.
        tracer.Mailbox.SendTracedSnapshot593(
            exactNativeValue,
            null,
            true,
            traceAdmission: false,
            publication593: publication
        );

        return true;
    }
}
