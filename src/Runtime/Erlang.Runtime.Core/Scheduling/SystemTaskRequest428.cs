namespace Erlang;

internal sealed class SystemTaskRequest428
{
    private PortableControlTerm826? requester826;
    private PortableControlTerm826? operation826;

    internal Term? Requester
    {
        get => requester826?.Value;
        private set => requester826 = value is null ? null : PortableControlTerm826.Capture(value);
    }

    internal TupleTerm? Operation
    {
        get => (TupleTerm?)operation826?.Value;
        private set => operation826 = value is null ? null : PortableControlTerm826.Capture(value);
    }
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
