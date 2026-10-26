namespace Erlang;

internal sealed class ManagedCollectorRequest413(
    ProcessContext requester,
    ProcessContext target,
    Term reply,
    bool trace
)
{
    internal ProcessContext? Requester = requester;
    internal ProcessContext? Target = target;
    internal Term? Reply = reply;
    internal bool Trace { get; } = trace;

    internal void Clear()
    {
        Requester = null;
        Target = null;
        Reply = null;
    }
}
