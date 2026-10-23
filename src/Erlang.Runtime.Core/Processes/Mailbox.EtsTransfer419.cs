namespace Erlang;

public sealed partial class Mailbox
{
    private readonly LinkedList<EtsAdmission419> etsQueueObservations419 = new();
    private readonly LinkedList<EtsAdmission419> etsCompletedQueueObservations419 = new();
    private int etsQueueObservationDepth419;

    internal EtsAdmission419 PrepareEtsTransfer419(ProcessContext recipient, Term message, Pid from)
    {
        if (!ReferenceEquals(recipient.Mailbox, this)) throw new ArgumentException(EtsHeirNames419.MailboxAdmission);
        IncomingTerm?.Invoke(message);
        IncomingTerm?.Invoke(from);
        BinarySharingCursor.Pin(message);

        return new(
            this,
            recipient,
            message,
            from
        );
    }

    // Called under existing mailbox mutation gate, outside the atomic owner publication transaction.
    // Later normal sends/removes must observe every earlier prepared count before their own.
    private void FlushEtsQueueObservations419()
    {
        List<Exception>? failures = null;
        while (etsQueueObservations419.First is { } first)
        {
            etsQueueObservations419.RemoveFirst();
            etsCompletedQueueObservations419.AddLast(first);
            try
            {
                first.Value.ObserveQueueCount();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }

    // Do not release a recipient termination fence in the middle of the subsequent
    // normal mutation callback: that callback can still record a high queue count.
    private void FinishEtsQueueObservations419()
    {
        if (--etsQueueObservationDepth419 != 0) return;
        while (etsCompletedQueueObservations419.First is { } first)
        {
            etsCompletedQueueObservations419.RemoveFirst();
            first.Value.FinishQueueCount();
        }
    }

    private void ObserveQueueLength419(int count)
    {
        etsQueueObservationDepth419++;
        try
        {
            Exception? first = null;
            try
            {
                FlushEtsQueueObservations419();
            }
            catch (Exception error)
            {
                first = error;
            }
            try
            {
                QueueLengthChanged?.Invoke(count);
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
            FinishEtsQueueObservations419();
        }
    }
}
