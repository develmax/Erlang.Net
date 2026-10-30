namespace Erlang;

internal sealed class PreparedMailboxMessage : IAsyncDisposable
{
    internal MailboxGraphSnapshot593 Graph { get; }
    internal Term Value { get; }

    internal PreparedMailboxMessage(MailboxGraphSnapshot593 graph, Term value)
    {
        Graph = graph;
        Value = value;
    }

    public ValueTask DisposeAsync() => Graph.DisposeAsync();
}
