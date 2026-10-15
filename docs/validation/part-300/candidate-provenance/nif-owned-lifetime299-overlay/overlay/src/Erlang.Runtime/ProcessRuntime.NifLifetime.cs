namespace Erlang;

public sealed partial class ProcessRuntime
{
    private TaskCompletionSource? automaticNifShutdown;

    private void StartNifShutdown()
    {
        lock (gate)
        {
            if (automaticNifShutdown is not null || Modules.NifLifetimeClosing)
                return;
            automaticNifShutdown = new(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                if (!ThreadPool.UnsafeQueueUserWorkItem(static (ProcessRuntime runtime) => runtime.RunAutomaticNifShutdown(), this, false))
                    throw new InvalidOperationException(nameof(ProcessRuntime));
            }
            catch (Exception failure)
            {
                automaticNifShutdown.TrySetException(failure);
            }
        }
    }

    private void RunAutomaticNifShutdown() => _ = CompleteAutomaticNifShutdown();

    private async Task CompleteAutomaticNifShutdown()
    {
        try
        {
            await Modules.CloseNifLifetimeAsync().ConfigureAwait(false);
            automaticNifShutdown!.TrySetResult();
        }
        catch (Exception failure)
        {
            automaticNifShutdown!.TrySetException(failure);
        }
    }

    private Task DrainAutomaticNifShutdown()
    {
        lock (gate)
            return automaticNifShutdown?.Task ?? Task.CompletedTask;
    }
}
