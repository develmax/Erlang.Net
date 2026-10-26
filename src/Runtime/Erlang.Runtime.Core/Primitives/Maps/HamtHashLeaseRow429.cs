namespace Erlang;

internal sealed class HamtHashLeaseRow429
{
    private RuntimeMapHashNodeLease[]? leases;
    private readonly WeakReference<HamtIndexOwner429> owner;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Completion => completion.Task;

    internal HamtHashLeaseRow429(RuntimeMapHashNodeLease[] leases, HamtIndexOwner429 owner)
    {
        this.leases = leases;
        this.owner = new(owner);
    }

    internal Task BeginRelease()
    {
        RuntimeMapHashNodeLease[]? owned = Interlocked.Exchange(ref leases, null);
        if (owned is null) return completion.Task;
        List<Exception>? failures = null;
        foreach (var lease in owned)
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        Exception? failure = failures is null ? null : new AggregateException(failures);
        // Record owner state before publishing completion. Its drain must not
        // race an unfinished failure notification from this exact row.
        if (owner.TryGetTarget(out var live)) live.LeaseReleased(this, failure);
        if (failure is null) completion.TrySetResult();
        else
        {
            completion.TrySetException(failure);
            _ = completion.Task.Exception;
        }

        return completion.Task;
    }

    internal void RecordUnexpectedFailure(Exception error)
    {
        try
        {
            if (owner.TryGetTarget(out var live)) live.LeaseReleased(this, error);
            completion.TrySetException(error);
            _ = completion.Task.Exception;
        }
        catch { }
    }
}
