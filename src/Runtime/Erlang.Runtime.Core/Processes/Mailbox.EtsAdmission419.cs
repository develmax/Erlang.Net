namespace Erlang;

public sealed partial class Mailbox
{
    internal sealed class EtsAdmission419
    {
        private readonly Mailbox owner;
        private readonly ProcessContext recipient;
        private readonly LinkedListNode<Entry> node;
        private readonly LinkedListNode<EtsAdmission419> observation;
        private readonly TaskCompletionSource nextSignal;
        private readonly TaskCompletionSource queueObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly LinkedListNode<Task> tracking;
        private readonly Term value, from;
        private bool committed;
        private int notified, queueCount;
        private Exception? observationFailure;

        internal EtsAdmission419(
            Mailbox owner,
            ProcessContext recipient,
            Term value,
            Term from
        )
        {
            this.owner = owner;
            this.recipient = recipient;
            this.value = value;
            this.from = from;
            node = new(new Entry(
                value,
                null,
                false,
                null,
                true,
                from
            ));
            observation = new(this);
            nextSignal = NewSignal();
            tracking = new(queueObserved.Task);
        }

        internal void Validate(Mailbox target)
        {
            if (!ReferenceEquals(owner, target) || !ReferenceEquals(recipient.Mailbox, target) || committed || node.List is not null)
                throw new InvalidOperationException(EtsHeirNames419.MailboxAdmission);
        }

        internal void Track(ProcessContext target)
        {
            if (!ReferenceEquals(target, recipient) || tracking.List is not null) throw new InvalidOperationException(EtsHeirNames419.MailboxAdmission);
            target.EtsQueueObservations419.AddLast(tracking);
        }

        internal void Commit(Mailbox target)
        {
            lock (owner.gate)
            {
                Validate(target);
                owner.messages.AddLast(node);
                owner.mutationRevision++;
                owner.admittedCount++;
                queueCount = owner.admittedCount;
                owner.etsQueueObservations419.AddLast(observation);
                var previous = owner.changed;
                owner.changed = nextSignal;
                committed = true;
                previous.TrySetResult();
            }
        }

        internal void ObserveQueueCount()
        {
            try
            {
                owner.QueueLengthChanged?.Invoke(queueCount);
            }
            catch (Exception error)
            {
                observationFailure = error;
                throw;
            }
        }

        internal void FinishQueueCount()
        {
            if (observationFailure is { } error) queueObserved.TrySetException(error); else queueObserved.TrySetResult();
        }

        internal void Notify()
        {
            if (!committed || Interlocked.Exchange(ref notified, 1) != 0) return;
            try
            {
                Exception? first = null;
                try
                {
                    lock (owner.gate)
                    {
                        owner.etsQueueObservationDepth419++;
                        try
                        {
                            owner.FlushEtsQueueObservations419();
                        }
                        finally
                        {
                            owner.FinishEtsQueueObservations419();
                        }
                    }
                }
                catch (Exception error)
                {
                    first = error;
                }
                try
                {
                    owner.MessageAdmitted?.Invoke(value, from);
                }
                catch (Exception error)
                {
                    if (first is not null) throw new AggregateException(first, error);
                    throw;
                }
                if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
            }
            finally
            {
                recipient.Runtime.ReleaseEtsQueueTracking419(recipient, tracking, queueObserved.Task);
            }
        }
    }
}
