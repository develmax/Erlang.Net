namespace Erlang;

internal sealed class DescriptorPollRuntimeLifetime558
{
    private readonly LinuxDescriptorPollService service;
    private readonly Func<Task> drainPorts;
    private readonly TaskCompletionSource closing = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal DescriptorPollRuntimeLifetime558(LinuxDescriptorPollService service, Func<Task> drainPorts)
    {
        this.service = service;
        this.drainPorts = drainPorts;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            Completion = Run();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal Task Completion { get; }

    internal bool CloseRequested => closing.Task.IsCompleted;

    internal void RequestClose() => closing.TrySetResult();

    private async Task Run()
    {
        await closing.Task.ConfigureAwait(false);
        Exception? portFailure = null;
        try
        {
            await drainPorts().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            portFailure = error;
        }
        try
        {
            await service.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error) when (portFailure is not null)
        {
            throw new AggregateException(portFailure, error);
        }
        if (portFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(portFailure).Throw();
    }
}
