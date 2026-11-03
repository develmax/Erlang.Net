namespace Erlang;

internal sealed class PreparedSendArguments593 : IDisposable, IAsyncDisposable
{
    private PreparedTermTransfer506? transfer;
    private MailboxPublicationScope593? publication;
    private int disposed;
    internal Term Target { get; }
    internal Term Message { get; }
    internal bool Detached { get; }
    internal Term OriginalTarget593 { get; private set; }
    internal Term OriginalMessage593 { get; private set; }
    internal MailboxPublicationScope593 Publication593 => publication
        ?? throw new InvalidOperationException(MailboxCustody593Names.MissingSendPublication);

    internal PreparedSendArguments593(Term target, Term message, bool detached = false)
    {
        Target = OriginalTarget593 = target;
        Message = OriginalMessage593 = message;
        Detached = detached;
    }

    internal PreparedSendArguments593(PreparedTermTransfer506 prepared)
    {
        if (prepared.Portable is not TupleTerm { Items.Count: 2 } pair)
            throw new InvalidDataException(MailboxCustody593Names.InvalidSendPair);
        Target = OriginalTarget593 = pair.Items[0];
        Message = OriginalMessage593 = pair.Items[1];
        Detached = true;
        transfer = prepared;
    }

    internal void SetOriginal593(Term target, Term message)
    {
        OriginalTarget593 = target;
        OriginalMessage593 = message;
    }

    internal void AttachPublication593(MailboxPublicationScope593 value)
    {
        if (publication is not null)
            throw new InvalidOperationException(MailboxCustody593Names.DuplicateSendPublication);
        publication = value;
    }

    public void Dispose() => DisposeCore593();

    public ValueTask DisposeAsync()
    {
        DisposeCore593();

        return ValueTask.CompletedTask;
    }

    private void DisposeCore593()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Exception? first = null, second = null;
        try
        {
            MailboxPublicationScope593? captured = Interlocked.Exchange(ref publication, null);
            captured?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            Interlocked.Exchange(ref transfer, null)?.Dispose();
        }
        catch (Exception error)
        {
            second = error;
        }
        if (first is not null || second is not null)
            throw new AggregateException(new[] { first, second }.OfType<Exception>());
    }
}
