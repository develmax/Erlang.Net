namespace Erlang;

internal sealed class InternalGroupLeaderRequest425(Pid requester, ReferenceTerm reference, Pid leader)
{
    internal Pid Requester { get; } = requester;
    internal ReferenceTerm Reference { get; } = reference;
    internal Pid Leader { get; } = leader;
}
