namespace Erlang;

internal sealed class ReadonlyTracePrefixNode817
{
    internal LinkedListNode<Mailbox.Entry> Node { get; }
    internal Mailbox.Entry Before { get; }
    internal ReadonlyTraceSource817? Source { get; }
    internal ReadonlyTraceOutput817? Output { get; set; }
    internal bool Priority817 { get; }
    private bool emitted;
    private bool admitted;

    internal ReadonlyTracePrefixNode817(LinkedListNode<Mailbox.Entry> node, ReadonlyTraceSource817? source, bool priority = false)
    {
        Node = node;
        Before = node.Value;
        Source = source;
        Priority817 = priority;
    }

    // Only the actual accepted transition can bind this node's event. The
    // Entry operation remains alive if a host consumes the accepted node later.
    internal void BindAccepted817(Mailbox.Entry actual)
    {
        if (admitted || !ReferenceEquals(actual.Value, Before.Value)
            || !ReferenceEquals(actual.Custody593, Before.Custody593))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        admitted = true;
    }

    // Exact accepted node, never a dictionary indexed by Value or equality.
    internal void Emit(ProcessTracing tracing, Mailbox mailbox)
    {
        if (!admitted || emitted || Source is null || Output is null)
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        emitted = true;
        using var scope = new ReadonlyTraceOutputScope817(tracing, Output);
        mailbox.EmitReadonlyAdmission817(Before.Value, Before.From);
        if (!scope.Consumed817) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
    }
}
