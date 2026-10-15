namespace Erlang;

internal sealed partial class NifNativeAllocationDomain
{
    private TaskCompletionSource? available;

    internal ValueTask<NifNativeAllocationDomainSnapshot> CaptureAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (TryCapture(out var captured))
            return new(captured);

        return CaptureContended(cancellation);
    }

    private bool TryCapture(out NifNativeAllocationDomainSnapshot captured)
    {
        if (!Monitor.TryEnter(gate))
        {
            captured = default;

            return false;
        }
        try
        {
            captured = new(owners.Count, snapshot);

            return true;
        }
        finally
        {
            ReleaseGate();
        }
    }

    private async ValueTask<NifNativeAllocationDomainSnapshot> CaptureContended(CancellationToken cancellation)
    {
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            TaskCompletionSource? notice = Volatile.Read(ref available);
            if (notice is null)
            {
                var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                notice = Interlocked.CompareExchange(ref available, candidate, null) ?? candidate;
            }
            // Recheck after publication: a prior release may have observed no waiter.
            if (TryCapture(out var captured))
                return captured;

            await notice.Task.WaitAsync(cancellation).ConfigureAwait(false);
        }
    }

    private void ReleaseGate()
    {
        Monitor.Exit(gate);
        // No allocation or interlocked exchange on an uncontended release.
        if (Volatile.Read(ref available) is not null)
            Interlocked.Exchange(ref available, null)?.TrySetResult();
    }
}
