namespace Erlang;

// One queue-owner reference plus temporary selector-operation references.
// Last release only schedules; it never runs backing callbacks under mailbox
// or runtime monitors. Registry capture validates actual typed producers.
internal sealed class MailboxPayloadLease593 : IThreadPoolWorkItem
{
    private readonly RuntimeMailboxRootOwner593 owner;
    internal RuntimeMailboxRootOwner593 Owner593 => owner;
    private IDisposable? backing;
    private MailboxMessageFragment? recipientFragment;
    internal MailboxMessageFragment? RecipientFragment => Volatile.Read(ref recipientFragment);
    private MailboxRetirementTracker593? expectedRecipient;
    internal Term? ObservationValue { get; private set; }

    internal void AttachRecipientPreparation(MailboxMessageFragment fragment, MailboxRetirementTracker593 recipient)
    {
        recipientFragment = fragment;
        expectedRecipient = recipient;
    }

    internal MailboxMessageFragment? TakeRecipientPreparation(MailboxRetirementTracker593 recipient)
        => ReferenceEquals(expectedRecipient, recipient) ? Interlocked.Exchange(ref recipientFragment, null) : null;

    internal void AttachRecipientFragment(MailboxMessageFragment fragment, Term? observationValue)
    {
        recipientFragment = fragment;
        ObservationValue = observationValue;
    }

    internal MailboxRetirementTracker593? RecipientTracker { get; private set; }

    internal void BindRecipient(MailboxRetirementTracker593 tracker)
    {
        if (RecipientTracker is not null) throw new InvalidOperationException(MailboxCustody593Names.AlreadyPublished);
        tracker.Register(this);
        RecipientTracker = tracker;
    }

    private int references = 1, ownerReleased, ready, executing, queueSettled;
    private Exception? schedulingFailure;
    internal readonly LinkedListNode<MailboxPayloadLease593> Link;
    internal readonly LinkedListNode<MailboxPayloadLease593> RecipientLink;
    internal readonly LinkedListNode<MailboxPayloadLease593> PublicationLink593;
    private int registered593, published593;
    internal Exception? ScopeCleanupFailure593 { get; set; }
    internal bool Registered593 => Volatile.Read(ref registered593) != 0;
    internal bool Published593 => Volatile.Read(ref published593) != 0;

    internal void MarkRegistered593() => Volatile.Write(ref registered593, 1);

    internal void MarkPublished593() => Volatile.Write(ref published593, 1);

    internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Ready => Volatile.Read(ref ready) != 0;
    internal bool Executing => Volatile.Read(ref executing) != 0;
    internal Exception? Failure { get; private set; }
    internal Exception? RetirementFailure { get; private set; }
    internal IEnumerable<Exception> Failures => new[]
    {
        Volatile.Read(ref schedulingFailure),
        RetirementFailure,
        Failure
    }
        .OfType<Exception>()
        .Distinct<Exception>(ReferenceEqualityComparer.Instance);

    internal MailboxPayloadLease593(RuntimeMailboxRootOwner593 owner)
    {
        this.owner = owner;
        Link = new(this);
        RecipientLink = new(this);
        PublicationLink593 = new(this);
    }

    internal MailboxGraphSnapshot593? Graph { get; private set; }

    internal void Attach(IDisposable? value) => backing = value;

    internal void AttachSnapshot(IDisposable value, MailboxGraphSnapshot593 graph)
    {
        backing = value;
        Graph = graph;
    }

    internal void CompleteEmptyCapture()
    {
        ownerReleased = 1;
        references = 0;
        ready = 1;
        queueSettled = 1;
        executing = 1;
        owner.Finished(this);
        Completion.TrySetResult();
    }

    internal IDisposable BorrowOperation()
    {
        var operation = new Operation(this);
        int old = Volatile.Read(ref references);
        while (true)
        {
            if (old <= 0) throw new ObjectDisposedException(nameof(MailboxPayloadLease593));
            int next = checked(old + 1);
            int observed = Interlocked.CompareExchange(ref references, next, old);
            if (observed == old)
            {
                return operation;
            }
            old = observed;
        }
    }

    internal void ReleaseOwner()
    {
        if (Interlocked.Exchange(ref ownerReleased, 1) == 0) Release();
    }

    private void Release()
    {
        int count = Interlocked.Decrement(ref references);
        if (count < 0) throw new InvalidOperationException(MailboxCustody593Names.NativeUnderflow);
        if (count != 0) return;
        Volatile.Write(ref ready, 1);
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false))
                throw new InvalidOperationException(MailboxCustody593Names.QueueFailed);
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref schedulingFailure, error, null);
            // Still retained by owner's actual active list; final drain retries
            // outside all runtime/mailbox gates and reports this failure.
        }
        finally
        {
            Volatile.Write(ref queueSettled, 1);
        }
        owner.NotifyReady();
        RecipientTracker?.NotifyReady();
    }

    public void Execute()
    {
        if (!Ready) return;
        // Queueing never waits for this worker. Settle its success/failure
        // before publishing retirement completion, including drain fallback.
        var spin = new SpinWait();
        while (Volatile.Read(ref queueSettled) == 0) spin.SpinOnce();
        if (Interlocked.CompareExchange(ref executing, 1, 0) != 0) return;
        if (Volatile.Read(ref recipientFragment) is null)
        {
            CompleteLegacyBacking();

            return;
        }
        if (ExecutionContext.IsFlowSuppressed()) _ = CompleteBackingAsync();
        else using (ExecutionContext.SuppressFlow()) _ = CompleteBackingAsync();
    }

    private void CompleteLegacyBacking()
    {
        Exception? retiredFailure = null;
        try
        {
            IDisposable? selected = Interlocked.Exchange(ref backing, null);
            if (selected is IMailboxRootInterest593 interest)
                MailboxRootSettlement593.Settle(interest.ReleaseLast());
            else
                NativeResourceRoots524.DisposeGuestGraph593(selected);
        }
        catch (Exception error)
        {
            retiredFailure = error;
        }
        RetirementFailure = retiredFailure;
        Exception? queued = Volatile.Read(ref schedulingFailure);
        Failure = queued ?? retiredFailure;
        if (queued is not null && retiredFailure is not null)
            try
            {
                Failure = new AggregateException(queued, retiredFailure);
            }
            catch (Exception aggregationFailure)
            {
                Failure = aggregationFailure;
            }
        owner.Finished(this);
        if (Failure is null)
        {
            Completion.TrySetResult();
        }
        else
        {
            Completion.TrySetException(Failure);
        }
    }


    private async Task CompleteBackingAsync()
    {
        // Both queued and drain fallback execution avoid the owning actor EC.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        Exception? retiredFailure = null;
        try
        {
            var fragment = Interlocked.Exchange(ref recipientFragment, null);
            if (fragment is not null) await fragment.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            retiredFailure = error;
        }
        try
        {
            IDisposable? selected = Interlocked.Exchange(ref backing, null);
            if (selected is IMailboxRootInterest593 interest)
                await MailboxRootSettlement593.SettleAsync(interest.ReleaseLast()).ConfigureAwait(false);
            else NativeResourceRoots524.DisposeGuestGraph593(selected);
        }
        catch (Exception error)
        {
            retiredFailure = retiredFailure is null ? error : new AggregateException(retiredFailure, error);
        }
        RetirementFailure = retiredFailure;
        Exception? queued = Volatile.Read(ref schedulingFailure);
        Failure = queued is null ? retiredFailure : retiredFailure is null ? queued
            : new AggregateException(queued, retiredFailure);
        try
        {
            owner.Finished(this);
        }
        catch (Exception error)
        {
            Failure = Failure is null ? error : new AggregateException(Failure, error);
        }
        if (Failure is null) Completion.TrySetResult();
        else Completion.TrySetException(Failure);
    }

    private sealed class Operation(MailboxPayloadLease593 value) : IDisposable
    {
        private MailboxPayloadLease593? current = value;

        public void Dispose() => Interlocked.Exchange(ref current, null)?.Release();
    }
}
