namespace Erlang;

internal sealed class RuntimeSchedulerOnlineRequest(ProcessContext owner, int requested)
{
    internal ProcessContext Owner { get; } = owner;
    internal int Requested { get; } = requested;
    internal int Previous { get; set; }
    internal LinkedListNode<RuntimeSchedulerOnlineRequest>? Node { get; set; }
    internal TaskCompletionSource Admitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
