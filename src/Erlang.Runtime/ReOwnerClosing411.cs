namespace Erlang;

internal sealed class ReOwnerClosing411(ProcessRuntime owner) : OperationCanceledException
{
    internal ProcessRuntime Owner { get; } = owner;
}
