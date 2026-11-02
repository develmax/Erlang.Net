namespace Erlang;

internal sealed class PreparedMailboxMessage : IAsyncDisposable
{
    internal MailboxGraphSnapshot593 Graph { get; }
    internal Term Value { get; }
    internal SequenceTraceToken? Token { get; }

    internal PreparedMailboxMessage(MailboxGraphSnapshot593 graph, Term value, SequenceTraceToken? token = null)
    {
        Graph = graph;
        Value = value;
        Token = token;
    }

    public ValueTask DisposeAsync() => Graph.DisposeAsync();
}
