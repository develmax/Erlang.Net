using System.Runtime.ExceptionServices;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Both normal owner and authorized dirty handler call this OUTSIDE R/M.
    // Legacy reflection-visible ClaimDeferred remains unchanged.
    private void PrepareAndCompleteDeferredSignal(ProcessContext owner, MailboxDeferredPreparation work)
    {
        DeferredMailboxAdmission? ready = null;
        PreparedMailboxMessage? prepared = null, sourceHold = null;
        MailboxPublicationScope593? publication = null;
        Exception? failure = null;
        bool releaseInvoked = false, completeInvoked = false;
        try
        {
            ready = work.Prepare(); // Preserve lazy decode and its actual ordering.
            ProcessReady();
        }
        catch (Exception error)
        {
            Add(error);
            ReleaseInput();
            if (!completeInvoked)
                try
                {
                    Complete(null);
                }
                catch (Exception cleanup)
                {
                    Add(cleanup);
                }
        }
        finally
        {
            ReleaseInput();
            ReleasePrepared();
            try
            {
                ready?.Payload641?.Close641();
            }
            catch (Exception cleanup)
            {
                Add(cleanup);
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();

        void Add(Exception error)
        {
            if (failure is null) failure = error;
            else if (!ReferenceEquals(failure, error)) failure = new AggregateException(failure, error);
        }

        bool Live()
            => !owner.Terminated
                && processes.TryGetValue(owner.Self, out var actual) && ReferenceEquals(actual, owner);

        void Complete(DeferredMailboxAdmission? value)
        {
            lock (gate)
            {
                completeInvoked = true; // Never retry a partly published Complete.
                work.Complete(Live() ? value : null);
            }
        }

        void ReleaseInput()
        {
            if (releaseInvoked) return;
            releaseInvoked = true; // A throwing arbitrary release is not replayed.
            try
            {
                work.Release?.Invoke();
            }
            catch (Exception cleanup)
            {
                Add(cleanup);
            }
        }

        void ReleasePrepared()
        {
            try
            {
                Interlocked.Exchange(ref publication, null)?.Dispose();
            }
            catch (Exception cleanup)
            {
                Add(cleanup);
            }
            try
            {
                var current = Interlocked.Exchange(ref prepared, null);
                if (current is not null) current.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                Add(cleanup);
            }
            try
            {
                var held = Interlocked.Exchange(ref sourceHold, null);
                if (held is not null) held.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                Add(cleanup);
            }
        }

        void ProcessReady()
        {
            if (ready is null || !ready.Copies.IsOne || !work.AllowBacking
                || !MailboxValueEscape.CanMaterialize(this, ready.Message))
            {
                ReleaseInput();
                if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
                Complete(ready);

                return;
            }
            Term source = ready.Message;
            while (true)
            {
                ProcessMessageQueueDataMode411 mode;
                long revision;
                lock (gate)
                {
                    if (!Live())
                    {
                        Complete(null);

                        return;
                    }
                    if (disposed || IsHaltRequested || !ProcessTracing.CanPublishDirectMailboxBacking())
                    {
                        // No physical copy is created for a known legacy trace path.
                        // Input is still held here unless a retry followed release.
                        if (releaseInvoked) break;
                        mode = default;
                        revision = -1;
                    }
                    else
                    {
                        mode = owner.Mailbox.MessageQueuePlacementMode;
                        revision = owner.Mailbox.MessageQueuePolicyRevision;
                    }
                }
                if (revision < 0)
                {
                    ReleaseInput();
                    if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
                    Complete(ready);

                    return;
                }
                try
                {
                    prepared = mailboxRoots593.PrepareMailboxMessageAsync(
                        this,
                        owner,
                        mode,
                        source
                    )
                        .GetAwaiter().GetResult();
                }
                catch (ObjectDisposedException)
                {
                    bool dead, closing;
                    lock (gate)
                    {
                        dead = !Live();
                        closing = disposed || IsHaltRequested;
                    }
                    if (dead)
                    {
                        Complete(null);

                        return;
                    }
                    if (!closing) throw;
                    // Fresh backing admission can lose to VM close. Preserve
                    // existing deferred admission semantics while its raw input
                    // or a previous retry graph still owns the decoded value.
                    if (sourceHold is not null) break;
                    ReleaseInput();
                    if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
                    Complete(ready);

                    return;
                }
                // New preparation now owns source/native custody, so any previous
                // retry holder can retire without leaving source a dangling view.
                if (sourceHold is not null)
                {
                    var old = sourceHold;
                    sourceHold = null;
                    old.DisposeAsync().GetAwaiter().GetResult();
                }
                publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                var actual = ready with { Message = prepared.Value, BackingPublication = publication };
                ReleaseInput(); // BEFORE admission, OUTSIDE R/M, exactly once.
                if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
                bool retry;
                lock (gate)
                {
                    if (!Live())
                    {
                        Complete(null);

                        return;
                    }
                    if (disposed || IsHaltRequested || !ProcessTracing.CanPublishDirectMailboxBacking()) break;
                    retry = owner.Mailbox.MessageQueuePlacementMode != mode
                        || owner.Mailbox.MessageQueuePolicyRevision != revision;
                    if (!retry)
                    {
                        Complete(actual);

                        return;
                    }
                }
                // Keep stable observation/native custody while building the next
                // attempt; never reread data after its raw decoder was released.
                source = prepared.Graph.ObservationValue!;
                publication.Dispose();
                publication = null;
                sourceHold = prepared;
                prepared = null;
            }
            // Trace policy changed after actual preparation. Drop only unqueued
            // physical backing outside gates, retain portable/native observation
            // for old trace delivery; physical trace allocation remains pending.
            var held = prepared ?? sourceHold!;
            if (held.Graph.RootWork593.TakeRecipientPreparation(owner.Mailbox.PayloadRetirements593) is { } fragment)
                fragment.DisposeAsync().GetAwaiter().GetResult();
            publication ??= MailboxPublicationScope593.Forward(this, held.Graph);
            var portable = ready with { Message = held.Graph.ObservationValue!, BackingPublication = publication };
            ReleaseInput();
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
            Complete(portable);
        }
    }
}
