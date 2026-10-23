namespace Erlang;

internal sealed class NifRetirementOperation(ModuleRegistry registry, ModuleCodeImage image, NifImageBindings bindings)
{
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal ModuleCodeImage Image => image;
    internal Task Completion => completion.Task;

    internal void Start()
    {
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(static (NifRetirementOperation operation) => operation.Run(), this, false))
                throw new InvalidOperationException(nameof(NifRetirementOperation));
        }
        catch (Exception failure)
        {
            registry.NifRetirementEnded(this, failure, completed: false);
            completion.TrySetException(failure);
        }
    }

    private void Run() => _ = Complete();

    private async Task Complete()
    {
        using var flow = NifRetirementFlow.Enter(registry, this);
        Exception? failure = null;
        try
        {
            await bindings.RetireAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            registry.NifRetirementEnded(this, failure, completed: true);
        }
        catch (Exception bookkeepingFailure)
        {
            failure = failure is null ? bookkeepingFailure : new AggregateException(failure, bookkeepingFailure);
        }
        if (failure is null)
            completion.TrySetResult();
        else
            completion.TrySetException(failure);
    }
}
