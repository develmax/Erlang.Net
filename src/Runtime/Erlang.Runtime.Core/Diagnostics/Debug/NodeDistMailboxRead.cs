namespace Erlang;

// Each operation pins the actual recipient work/fragment, including while a
// process exits. Capture only increments custody interests under M; all reads
// and releases run after M, outside the runtime monitor.
internal sealed class NodeDistMailboxRead(int capacity) : IDisposable
{
    private readonly (Term Value, MailboxMessageFragment? Fragment, IDisposable? Borrow)[] entries = new (Term, MailboxMessageFragment?, IDisposable?)[capacity];
    private int count;
    internal Exception? Failure { get; set; }

    internal void Add(Term value, MailboxEntryCustody593? custody)
    {
        IDisposable? borrowed = custody?.BorrowOperation();
        entries[count++] = (value, custody?.RecipientFragment, borrowed);
    }

    internal void Capture(NodeDistReferenceSnapshot snapshot, Term owner)
    {
        if (Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        for (int index = 0; index < count; index++)
        {
            var entry = entries[index];
            string kind = entry.Fragment?.OwnsHeap == true ? NodeDistReferenceNames.Signal : NodeDistReferenceNames.Heap;
            if (entry.Fragment?.OwnsHeap == true)
                entry.Fragment.HeapOwner.CaptureNodeDistReferences(snapshot, owner, kind);
            snapshot.Graph(entry.Value, owner, kind);
        }
    }

    public void Dispose()
    {
        List<Exception>? errors = null;
        for (int index = 0; index < count; index++)
        {
            var entry = entries[index];
            entries[index] = default;
            try
            {
                entry.Borrow?.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        }
        count = 0;
        if (errors is not null) throw new AggregateException(errors);
    }
}
