namespace Erlang;

internal sealed class DistributionOutputWake457
{
    private readonly object gate = new();
    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task CaptureBeforeQueueCheck()
    {
        lock (gate)
        {
            if (changed.Task.IsCompleted) changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

            return changed.Task;
        }
    }

    internal void Wake()
    {
        lock (gate) changed.TrySetResult();
    }
}
