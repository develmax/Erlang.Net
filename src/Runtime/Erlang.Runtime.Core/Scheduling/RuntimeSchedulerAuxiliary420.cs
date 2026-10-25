namespace Erlang;

internal sealed class RuntimeSchedulerAuxiliary420
{
    private readonly Action<int> body;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal LinkedListNode<RuntimeSchedulerAuxiliary420> Node { get; }
    internal Task Completion => completion.Task;

    internal RuntimeSchedulerAuxiliary420(Action<int> body)
    {
        this.body = body;
        Node = new(this);
    }

    internal void Execute(int lane)
    {
        try
        {
            body(lane);
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }
    }

    internal void Fail(Exception error) => completion.TrySetException(error);
}
