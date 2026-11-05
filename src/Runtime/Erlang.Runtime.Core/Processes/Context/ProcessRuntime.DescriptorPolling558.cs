namespace Erlang;

public sealed partial class ProcessRuntime
{
    private LinuxDescriptorPollService? descriptorPolling558;
    private DescriptorPollRuntimeLifetime558? descriptorPollingLifetime558;

    internal LinuxDescriptorPollService? DescriptorPolling558 => descriptorPolling558;

    private void StartDescriptorPolling558()
    {
        if (!OperatingSystem.IsLinux()) return;
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(RuntimeWorkerStartupNames.RuntimeGate);
        descriptorPolling558 = new(Statistics.Microstates, workerCreation);
        descriptorPollingLifetime558 = new(descriptorPolling558, Ports.Drain);
        descriptorPolling558.Start();
        descriptorPolling558.Ready.GetAwaiter().GetResult();
    }

    private void BeginDescriptorPollingShutdown558()
        => descriptorPollingLifetime558?.RequestClose();

    private Task DrainDescriptorPolling558()
        => descriptorPollingLifetime558 is { CloseRequested: true } owner ? owner.Completion : Task.CompletedTask;

    private Task JoinDescriptorPollingShutdown558(Task other)
    {
        Task polling = DrainDescriptorPolling558();
        if (ReferenceEquals(polling, Task.CompletedTask)) return other;

        return Join(other, polling);

        static async Task Join(Task first, Task second)
        {
            Task all = Task.WhenAll(first, second);
            try
            {
                await all.ConfigureAwait(false);
            }
            catch when (all.Exception is { InnerExceptions.Count: > 1 })
            {
                throw all.Exception;
            }
        }
    }

    private async Task StopDescriptorPollingForRollback558()
    {
        if (descriptorPolling558 is not { } service) return;
        Exception? shutdown = null;
        try
        {
            lock (gate) Ports?.Shutdown();
        }
        catch (Exception error)
        {
            shutdown = error;
        }
        try
        {
            if (descriptorPollingLifetime558 is { } owner)
            {
                owner.RequestClose();
                await owner.Completion.ConfigureAwait(false);
            }
            else
            {
                await service.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception error) when (shutdown is not null)
        {
            throw new AggregateException(shutdown, error);
        }
        if (shutdown is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(shutdown).Throw();
    }
}
