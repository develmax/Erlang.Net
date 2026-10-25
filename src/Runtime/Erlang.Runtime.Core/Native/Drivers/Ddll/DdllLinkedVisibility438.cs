namespace Erlang;

internal sealed class DdllLinkedVisibility438
{
    internal DdllLinkedVisibility438(DdllParentRow422 owner, ErlDriverLinkedScope438 scope)
    {
        Owner = owner;
        Scope = scope;
    }

    internal DdllParentRow422 Owner { get; }
    internal ErlDriverLinkedScope438 Scope { get; }
    internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
