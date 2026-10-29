namespace Erlang;

// Independent queue ownership, not a public process-memory metric. No callback
// or runtime/heap acquisition occurs while this small owner monitor is held.
internal sealed partial class RuntimeMailboxRootOwner593
{
    private readonly object gate = new();
    private readonly LinkedList<MailboxPayloadLease593> active = new(), failed = new();
    private readonly SemaphoreSlim changed = new(0, 1);
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closed;
    private Task? drain;

    internal MailboxPayloadLease593? Capture(ProcessRuntime runtime, Term message)
    {
        var work = new MailboxPayloadLease593(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
        }
        bool emptyCompleted = false;
        try
        {
            IDisposable? roots = NativeResourceRoots524.RetainGraph524(message, runtime);
            if (roots is null)
            {
                emptyCompleted = true;
                work.CompleteEmptyCapture();

                return null;
            }
            work.Attach(roots);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return work;
        }
        catch (Exception original)
        {
            if (!emptyCompleted)
            {
                work.ReleaseOwner();
                // Capture is a BEFORE-runtime/mailbox-monitor operation. Do
                // not abandon its specific cleanup job after a capture failure.
                try
                {
                    MailboxRootSettlement593.Settle(work);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(original, cleanup);
                }
            }

            throw;
        }
    }

    internal void StopAdmission()
    {
        lock (gate)
        {
            closed = true;
            if (active.Count == 0) drained.TrySetResult();
        }
        NotifyReady();
    }

    internal void NotifyReady()
    {
        try
        {
            changed.Release();
        }
        catch (SemaphoreFullException) { }
    }

    internal void Finished(MailboxPayloadLease593 work)
    {
        lock (gate)
        {
            if (!ReferenceEquals(work.Link.List, active)) throw new InvalidOperationException(MailboxCustody593Names.WrongOwner);
            active.Remove(work.Link);
            if (work.Failure is not null) failed.AddLast(work.Link);
            if (closed && active.Count == 0) drained.TrySetResult();
        }
        work.RecipientTracker?.Finished(work);
        NotifyReady();
    }

    internal Task DrainAsync()
    {
        lock (gate)
        {
            if (drain is not null) return drain;
            if (ExecutionContext.IsFlowSuppressed()) return drain = DrainCoreAsync();
            using (ExecutionContext.SuppressFlow()) return drain = DrainCoreAsync();
        }
    }

    private async Task DrainCoreAsync()
    {
        // Shutdown may be synchronously joined by an actor of another VM.
        // Never require that actor's scheduler or inherit its execution context.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        StopAdmission();
        while (!drained.Task.IsCompleted)
        {
            MailboxPayloadLease593? retry = null;
            lock (gate)
                for (var node = active.First; node is not null; node = node.Next)
                    if (node.Value.Ready && !node.Value.Executing)
                    {
                        retry = node.Value;
                        break;
                    }
            if (retry is not null)
            {
                retry.Execute();
                continue;
            }
            await changed.WaitAsync().ConfigureAwait(false);
        }
        await drained.Task.ConfigureAwait(false);
        Exception[] failures;
        lock (gate) failures = failed.SelectMany(work => work.Failures).ToArray();
        if (failures.Length != 0) throw new AggregateException(failures);
    }
}
