namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    private Task? suspensionAckCleanup172;
    private SemaphoreSlim? suspensionAckChanged172;

    private void EnsureSuspensionAckCleanup172()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (suspensionAckCleanup172 is not null) return;
            suspensionAckChanged172 ??= new SemaphoreSlim(0);
            if (ExecutionContext.IsFlowSuppressed()) suspensionAckCleanup172 = RunSuspensionAckCleanup172();
            else using (ExecutionContext.SuppressFlow()) suspensionAckCleanup172 = RunSuspensionAckCleanup172();
        }
    }

    internal void NotifySuspensionAck172()
    {
        try
        {
            Volatile.Read(ref suspensionAckChanged172)?.Release();
        }
        catch (SemaphoreFullException) { }
    }

    // Actual registered ACK work only. This live fallback is independent of
    // per-work ThreadPool queue success; final Drain retains failure reporting.
    private async Task RunSuspensionAckCleanup172()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        try
        {
            while (true)
            {
                MailboxPayloadLease593? ready = null;
                lock (gate)
                {
                    if (closed) return;
                    for (var node = active.First; node is not null; node = node.Next)
                        if (node.Value.SuspensionAckCleanup172 && node.Value.Ready && !node.Value.Executing)
                        {
                            ready = node.Value;
                            break;
                        }
                }
                if (ready is not null)
                {
                    ready.Execute();
                    continue;
                }
                await suspensionAckChanged172!.WaitAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            // The ACK pump has its own wake-up source; generic shutdown
            // notification is never consumed by this pump.
            NotifyReady();
        }
    }
}
