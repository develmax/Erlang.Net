namespace Erlang;

// Identical finite observation on both source versions. This host-controlled
// gate pauses after an actual EMPTY snapshot is built, before the actual poll.
internal sealed class LinuxDescriptorPollProbe1557 : IDisposable
{
    private readonly ManualResetEventSlim release = new();
    private readonly TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int selected;

    internal Task Reached => reached.Task;

    internal void BeforeNativePoll(int snapshotCount)
    {
        if (snapshotCount != 0 || Interlocked.CompareExchange(ref selected, 1, 0) != 0) return;
        reached.TrySetResult();
        release.Wait();
    }

    internal void Release() => release.Set();

    public void Dispose() => release.Dispose(); // Caller joins actual engine thread first.
}
