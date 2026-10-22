namespace Erlang;

internal sealed class ProcessAliveRequest425(Pid requester, ReferenceTerm reference)
{
    internal Pid Requester { get; } = requester;
    internal ReferenceTerm Reference { get; } = reference;
}
