namespace Erlang;

// Existing queue custody is the ONLY native source owner during reservation.
// A source use is metadata interest in that exact operation, not a BinRef +1.
internal sealed class ReadonlyTraceSource817 : IDisposable
{
    private readonly ProcessContext receiver;
    private readonly MailboxMessageFragment fragment;
    private IDisposable? operation;
    private int references = 1;
    private int released;
    private Dictionary<BitString, ReadonlyTraceBinary817>? binaries;
    internal ProcessRuntime Runtime => receiver.Runtime;
    internal ProcessContext Receiver817 => receiver;
    internal Term Value { get; }

    private ReadonlyTraceSource817(
        ProcessContext receiver,
        MailboxMessageFragment fragment,
        Term value,
        IDisposable operation
    )
    {
        this.receiver = receiver;
        this.fragment = fragment;
        Value = value;
        this.operation = operation;
    }

    // Called with the original Entry still owned (normally R/M). No native
    // field, arbitrary source graph or new image authority is inspected here.
    internal static ReadonlyTraceSource817? CaptureEntry(
        ProcessContext receiver,
        MailboxEntryCustody593 custody,
        Term exactEntryValue
    )
    {
        if (!custody.IsRecipient817(receiver.Mailbox.PayloadRetirements593)
            || custody.RecipientFragment is not { } fragment
            || !ReferenceEquals(fragment.Value, exactEntryValue)
            || !ReferenceEquals(fragment.HeapOwner.Runtime524, receiver.Runtime)
            || !fragment.OwnsHeap && !ReferenceEquals(fragment.HeapOwner, receiver.TermHeap506)) return null;
        IDisposable operation = custody.BorrowOperation();
        try
        {
            return new(
                receiver,
                fragment,
                exactEntryValue,
                operation
            );
        }
        catch
        {
            operation.Dispose();
            throw;
        }
    }

    internal bool ValidateOutsideGates()
    {
        Runtime.AssertSequenceTracePreparation();
        if (receiver.Mailbox.DeferredGateHeld) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref operation) is null, this);
        if (!fragment.HasSingleReadonlyValueSlot817()) return false;
        var found = new Dictionary<BitString, ReadonlyTraceBinary817>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(Value);
        int remaining = ReadonlyTrace817Names.MaximumSourceNodes;
        ProcessTermHeap506 heap = fragment.HeapOwner;
        lock (heap.Gate)
        {
            while (pending.TryPop(out Term? value))
            {
                if (!seen.Add(value)) continue;
                if (--remaining < 0) return false;
                if (value is Atom or Nil || value is Integer integer && ProcessTermHeap506.IsImmediateInteger559(integer))
                    continue;
                if (ProcessTermHeap506.Storage(value) is not ProcessTermSequence506 block
                    || !ReferenceEquals(block.Owner, heap) || heap.LiteralDomain800 is not null
                    || !heap.IsOwnedLocal(value) || block.IsMutableSequenceToken814) return false;
                if (value is BitString bits)
                {
                    if (!heap.TryCaptureReadonlyTraceBinary817(bits, out ReadonlyTraceBinary817? captured)) return false;
                    found.Add(bits, captured!);
                }
                else if (value is Cons cons)
                {
                    pending.Push(cons.Tail);
                    pending.Push(cons.Head);
                }
                else if (value is TupleTerm tuple)
                {
                    if (tuple.Items.Count > ReadonlyTrace817Names.MaximumSourceNodes) return false;
                    for (int index = tuple.Items.Count - 1; index >= 0; index--) pending.Push(tuple.Items[index]);
                }
                else return false;
            }
        }
        binaries = found;

        return true;
    }

    internal ReadonlyTraceSource817 Retain()
    {
        int old = Volatile.Read(ref references);
        while (old > 0)
        {
            int next = checked(old + 1);
            int observed = Interlocked.CompareExchange(ref references, next, old);
            if (observed == old) return this;
            old = observed;
        }
        throw new ObjectDisposedException(nameof(ReadonlyTraceSource817));
    }

    internal ReadonlyTraceBinary817 Binary(BitString value)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref operation) is null, this);
        if (binaries is null || !binaries.TryGetValue(value, out ReadonlyTraceBinary817? result))
            throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);

        return result;
    }

    internal void RequireValidated()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref operation) is null, this);
        if (binaries is null) throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);
    }

    internal void ReleaseUse()
    {
        int remaining = Interlocked.Decrement(ref references);
        if (remaining < 0) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        if (remaining == 0) Interlocked.Exchange(ref operation, null)?.Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref released, 1) == 0) ReleaseUse();
    }
}
