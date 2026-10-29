namespace Erlang;

// Intrusive internal test instrumentation. Default-null owner attachment is the
// production route. This object owns synchronization DATA, never guest terms,
// resource leases, arbitrary delegates or a Timer to invoke manually.
internal sealed class RuntimeTimerCallbackPhase539 : IDisposable
{
    private readonly ManualResetEventSlim release = new(false);
    private readonly TaskCompletionSource claimed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private object? runtimeGate;
    private int claims;

    internal Task Claimed539 => claimed.Task;
    internal Task Exited539 => exited.Task;
    internal int Claims539 => Volatile.Read(ref claims);

    internal void Bind539(object gate) => runtimeGate = gate;

    // Called by the actual System.Threading.Timer callback AFTER it removed its
    // current Entry/registered retirement and LEFT R. No test callback executes.
    internal void WaitAfterClaim539()
    {
        if (runtimeGate is null || Monitor.IsEntered(runtimeGate))
            throw new InvalidOperationException(RuntimeTimerCallbackPhase539Names.UnderGate);
        int count = Interlocked.Increment(ref claims);
        claimed.TrySetResult();
        if (count != 1)
            throw new InvalidOperationException(RuntimeTimerCallbackPhase539Names.DuplicateClaim);
        if (!release.Wait(RuntimeTimerCallbackPhase539Names.Watchdog))
            throw new TimeoutException(RuntimeTimerCallbackPhase539Names.ReleaseMissing);
    }

    internal void Release539() => release.Set();

    internal void SignalExited539() => exited.TrySetResult();

    // Test owner calls this only after actual callback exit/runtime drain.
    public void Dispose() => release.Dispose();
}
