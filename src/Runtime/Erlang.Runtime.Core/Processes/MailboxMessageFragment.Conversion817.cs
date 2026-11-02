namespace Erlang;

internal sealed partial class MailboxMessageFragment
{
    // Caller holds the exact queued Entry operation before entering here.
    // The actual fragment frame, not a bare foreign term, grants this borrow.
    internal ProcessTermBorrow506 BorrowValueForConversion817(Term exactValue)
    {
        if (!ReferenceEquals(Value, exactValue))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        ProcessTermFrame506 current = Volatile.Read(ref frame)
            ?? throw new ObjectDisposedException(nameof(MailboxMessageFragment));

        return heap.Borrow(current, exactValue);
    }
}
