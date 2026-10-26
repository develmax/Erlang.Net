namespace Erlang;

internal sealed class ProcessAliveRequest425(Pid requester, ReferenceTerm? reference, TaskCompletionSource<bool>? completion = null)
{
    internal Pid Requester { get; } = requester;
    internal ReferenceTerm? Reference { get; } = reference;
    internal TaskCompletionSource<bool>? Completion { get; } = completion;
}
