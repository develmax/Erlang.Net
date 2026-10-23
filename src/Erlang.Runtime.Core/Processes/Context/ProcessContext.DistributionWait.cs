namespace Erlang;

public sealed partial class ProcessContext
{
    internal async ValueTask WaitForDistributionControlCompletionAsync(Task completion, CancellationToken cancellation)
    {
        Runtime.ThrowIfHalted();
        cancellation.ThrowIfCancellationRequested();
        await Mailbox.WaitForControlCompletion(completion, cancellation).ConfigureAwait(false);
        // Completion may already have been ready: admit queued controls before the next publication phase.
        Runtime.AdmitSignals(this);
        Runtime.ThrowIfHalted();
        cancellation.ThrowIfCancellationRequested();
        if (SuspensionRequested)
            await WaitIfSuspended().ConfigureAwait(false);
        Runtime.ThrowIfHalted();
        cancellation.ThrowIfCancellationRequested();
    }
}
