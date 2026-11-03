namespace Erlang.Tests;

// Failure-path containment only; primary checks explicitly inspect expected failures.
internal sealed class WorkerLifecycleOwner(object owner, bool dirty) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        Task close = (Task)WorkerLifecycleAccess.Call(owner, dirty ? "BeginClose" : "CompleteWhenIdle");
        try
        {
            await close.WaitAsync(WorkerLifecycleNames.Deadline);
        }
        catch (Exception) when (close.IsCompleted)
        {
            // Expected erroring test owners remain faulted; never fabricate success.
            // The test body verifies full error markers. Physical joins remain required.
        }
        WorkerLifecycleAccess.Joined(owner, dirty);
    }
}
