namespace Erlang;

internal sealed class SystemTaskRequest428
{
    internal Term? Requester { get; private set; }
    internal TupleTerm? Operation { get; private set; }
    internal Term? Id => Operation?.Items[1];
    internal string Kind { get; }
    internal ProcessSchedulingPriority Priority { get; }
    internal LinkedListNode<SystemTaskRequest428> PendingNode { get; }
    internal LinkedListNode<SystemTaskRequest428> ReadyNode { get; }
    internal TaskCompletionSource PhysicalDone { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal SystemTaskRequest428(
        Term requester,
        TupleTerm operation,
        string kind,
        ProcessSchedulingPriority priority
    )
    {
        Requester = requester;
        Operation = operation;
        Kind = kind;
        Priority = priority;
        PendingNode = new(this);
        ReadyNode = new(this);
    }

    internal void Clear()
    {
        Requester = null;
        Operation = null;
    }
}
