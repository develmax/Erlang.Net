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

    public void Dispose() => Interlocked.Exchange(ref operation, null)?.Dispose();
}
