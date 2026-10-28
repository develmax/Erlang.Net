namespace Erlang;

// Joins this mailbox's ROOT-release work only, not downstream native
// destructor completion or physical atomics free. Those owners drain separately.
internal sealed class MailboxRetirementTracker593
{
    private readonly object gate = new();
    private readonly LinkedList<MailboxPayloadLease593> active = new(), failed = new();
    private readonly SemaphoreSlim changed = new(0, 1);
    private Task? drain;
    private bool closed;

    internal bool NeedsDrain593
    {
        get
        {
            lock (gate) return active.Count != 0 || failed.Count != 0;
        }
    }

    internal void Register(MailboxPayloadLease593 work)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.RecipientLink);
        }
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
            if (!ReferenceEquals(work.RecipientLink.List, active))
                throw new InvalidOperationException(MailboxCustody593Names.WrongTracker);

            active.Remove(work.RecipientLink);
            if (work.Failure is not null)
                failed.AddLast(work.RecipientLink);
        }

        NotifyReady();
    }

    // May be latched under a mailbox/runtime monitor; does not release roots.
    internal void Close()
    {
        lock (gate) closed = true;
        NotifyReady();
    }

    // Must be awaited by the existing outside-monitor exit continuation.
    // The initial yield also prevents release callbacks from running inline
    // even if a future caller forgets that placement prerequisite.
    internal Task DrainAsync()
    {
        lock (gate)
            return drain ??= DrainCoreAsync();
    }

    private async Task DrainCoreAsync()
    {
        await Task.Yield();
        Close();
        while (true)
        {
            MailboxPayloadLease593? retry = null;
            lock (gate)
            {
                if (active.Count == 0)
                    break;

                for (var node = active.First; node is not null; node = node.Next)
                    if (node.Value.Ready && !node.Value.Executing)
                    {
                        retry = node.Value;
                        break;
                    }
            }

            if (retry is not null)
            {
                retry.Execute();
                continue;
            }

            await changed.WaitAsync().ConfigureAwait(false);
        }

        Exception[] failures;
        lock (gate)
            failures = failed.SelectMany(work => work.Failures).ToArray();

        if (failures.Length != 0)
            throw new AggregateException(failures);
    }
}
