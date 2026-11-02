namespace Erlang;

// Actual queue node plus existing queue-custody operation, never a guest term
// wrapper or new execution authority. Only the real signal owner consumes it.
internal sealed class MailboxPendingBackingClaim : IDisposable
{
    internal Mailbox Owner { get; }
    internal LinkedListNode<Mailbox.Entry> Node { get; }
    internal Mailbox.Entry Original { get; }
    internal Term Value => Original.Value;
    private IDisposable? operation;

    internal MailboxPendingBackingClaim(Mailbox owner, LinkedListNode<Mailbox.Entry> node)
    {
        Owner = owner;
        Node = node;
        Original = node.Value;
    }

    internal void RetainSource() => operation = Original.Custody593?.BorrowOperation();

    // Exact selected Entry custody, never inferred from the drain actor.
    internal MailboxPendingBackingClaim? RetainedFragmentSource
        => Volatile.Read(ref operation) is not null
            && Original.Custody593?.RecipientFragment is { } actual
            && ReferenceEquals(actual.Value, Value) ? this : null;

    internal ProcessTermBorrow506 BorrowPreparationSource(
        ProcessRuntime runtime,
        Term exactValue,
        SequenceTraceToken? exactToken,
        Term? exactRawToken
    )
    {
        if (Volatile.Read(ref operation) is null || !ReferenceEquals(exactValue, Value)
            || !ReferenceEquals(exactToken, Original.TraceToken)
            || !ReferenceEquals(exactRawToken, Original.DistributionTraceToken)
            || Original.Custody593?.RecipientFragment is not { } actual
            || !ReferenceEquals(actual.Value, exactValue)
            || !ReferenceEquals(actual.HeapOwner.Runtime524, runtime)
            || exactToken is not null && !ReferenceEquals(actual.Token, exactToken))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);

        return actual.BorrowValueForConversion817(exactValue);
    }

    public void Dispose() => Interlocked.Exchange(ref operation, null)?.Dispose();
}
