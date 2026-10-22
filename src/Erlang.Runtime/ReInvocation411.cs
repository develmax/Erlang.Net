namespace Erlang;

internal sealed class ReInvocation411
{
    internal int Count;
    internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
