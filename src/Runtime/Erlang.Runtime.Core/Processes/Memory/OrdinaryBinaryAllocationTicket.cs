namespace Erlang;

internal sealed class OrdinaryBinaryAllocationTicket(RuntimeOrdinaryBinaryOwner owner)
{
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int complete;
    internal Task Completion => settled.Task;

    internal void Complete()
    {
        if (Interlocked.Exchange(ref complete, 1) != 0) return;
        settled.TrySetResult();
        owner.Forget(this);
    }
}
