namespace Erlang;

internal sealed class DistributionConnectedEntry(
    HostDistribution queue,
    HostDistributionHandle queueHandle,
    ProcessContext controller,
    ulong flags,
    uint remoteCreation
)
{
    internal HostDistribution Queue { get; } = queue;
    internal DistributionOutputSequencer Output { get; } = new();
    internal Task SystemOutput { get; set; } = Task.CompletedTask;

    internal HostDistributionHandle QueueHandle { get; } = queueHandle;
    internal ProcessContext Controller { get; } = controller;
    internal ulong Flags { get; } = flags;
    internal uint RemoteCreation { get; } = remoteCreation;
    internal Pid? InputHandler { get; set; }
    internal long RawInputAttempts { get; set; }
}
