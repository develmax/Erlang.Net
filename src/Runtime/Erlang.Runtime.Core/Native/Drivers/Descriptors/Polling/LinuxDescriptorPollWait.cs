namespace Erlang;

internal sealed class LinuxDescriptorPollWait(
    LinuxDescriptorPollOwner owner,
    short events,
    CancellationToken cancellation
)
{
    internal readonly LinuxDescriptorPollOwner Owner = owner;
    internal readonly short Events = events;
    internal readonly CancellationToken Cancellation = cancellation;
    internal readonly TaskCompletionSource<short> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal CancellationTokenRegistration CancellationRegistration;
    internal ulong Generation;
    internal bool Admitted;
    internal bool CancellationRequested;
    internal bool CancelResult;
    internal bool SelectionComplete;
    internal short ReadyEvents;
    internal Exception? Error;
}
