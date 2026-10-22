namespace Erlang;

public sealed partial class Mailbox
{
    internal EtsAdmission419 PrepareEtsAll420(ProcessContext recipient, Term message)
    {
        if (!ReferenceEquals(recipient.Mailbox, this)) throw new ArgumentException(EtsHeirNames419.MailboxAdmission);
        IncomingTerm?.Invoke(message);
        BinarySharingCursor.Pin(message);

        return new(
            this,
            recipient,
            message,
            EtsAllNames420.Sender
        );
    }
}
