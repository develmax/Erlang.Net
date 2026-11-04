namespace Erlang;

// Borrowed FD identity only. Retirement fences this engine's poll snapshots,
// not external close/reuse or the caller's later blocking data transfer.
internal sealed class LinuxDescriptorPollOwner
{
    internal LinuxDescriptorPollOwner(LinuxDescriptorPollService service, ulong generation, int descriptor)
    {
        Service = service;
        Generation = generation;
        Descriptor = descriptor;
    }

    internal LinuxDescriptorPollService Service { get; }
    internal ulong Generation { get; }
    internal int Descriptor { get; }
    internal bool Closing;
    internal int Pending;
    internal readonly ObjectDisposedException Closed = new(nameof(LinuxDescriptorPollOwner));
    internal readonly TaskCompletionSource Retired = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task BeginRetire() => Service.Retire(this);
}
