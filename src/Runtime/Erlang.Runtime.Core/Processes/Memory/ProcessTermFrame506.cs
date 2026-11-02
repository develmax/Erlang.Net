namespace Erlang;

internal sealed partial class ProcessTermFrame506 : IDisposable
{
    private readonly ProcessTermHeap506 owner;
    private readonly Dictionary<int, Term> values = new();
    private int nextSlot;
    private bool disposed;

    internal ProcessTermFrame506(ProcessTermHeap506 owner) => this.owner = owner;

    internal IEnumerable<Term> Roots => values.Values;
    internal bool IsDisposed => disposed;

    internal void InitializeBorrow506(Term value)
    {
        nextSlot = 1;
        PublishRoot512(0, value, false);
    }

    internal int Reserve()
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            return checked(nextSlot++);
        }
    }

    // Two logical mirror identifiers are admitted together before publication.
    // Reserving IDs does not allocate or account for guest heap words.
    internal (int First, int Second) ReservePair507()
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            int first = nextSlot;
            int second = checked(first + 1);
            int after = checked(second + 1);
            nextSlot = after;

            return (first, second);
        }
    }

    internal void RequireLive507()
    {
        lock (owner.Gate)
            ObjectDisposedException.ThrowIf(disposed, this);
    }

    internal void Set(int slot, Term value) => SetCore414(slot, value, null);

    internal void SetPublicInput414(int slot, Term value, PublicReceiverFunction414 receiver)
        => SetCore414(slot, value, receiver);

    private void SetCore414(int slot, Term value, PublicReceiverFunction414? receiver)
    {
        lock (owner.Gate) ValidateSlot(slot);
        if (TryPublishImmediate157(
            slot,
            value,
            detachedCopy: true,
            requireOutsideHeap: true
        )) return;
        value = owner.CanonicalizeEmptyTuple803(value);
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(owner, value);
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            using var literalAdmission = owner.EnterLiteralAdmission801(literalRoots);
            if (owner.IsAuthenticatedLiteral801(value) || owner.IsOwnedLocal(value))
            {
                PublishRoot512(slot, value);

                return;
            }
        }
        using PreparedTermTransfer506 prepared = receiver is null
            ? owner.Prepare(this, value) : owner.PreparePublicFunctions414(this, value, receiver);
        using PreparedHeapImport imported = owner.PrepareHeapImport(this, prepared.Portable);
        SetLocalGraph507(slot, imported.Value);
    }

    internal void SetPrepared(int slot, Term portable) => SetPreparedWithLiteral801(slot, portable, null);

    internal void SetPreparedWithLiteral801(int slot, Term portable, ModuleLiteralRoots801? carriedLiteral801)
    {
        lock (owner.Gate) ValidateSlot(slot);
        if (TryPublishImmediate157(
            slot,
            portable,
            detachedCopy: false,
            requireOutsideHeap: true
        )) return;
        using PreparedHeapImport imported = owner.PrepareHeapImportWithLiteral801(this, portable, carriedLiteral801);
        SetLocalGraph507(slot, imported.Value);
    }

    internal PreparedHeapImport PrepareImport(Term portable) => owner.PrepareHeapImport(this, portable);

    private void ValidateSlot(int slot)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (slot < 0 || slot >= nextSlot)
            throw new ArgumentOutOfRangeException(nameof(slot));
    }

    internal Term Read(int slot)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            return values[slot];
        }
    }

    // Existing live root only; this grants no new ownership/admission. Callers
    // separately require the actual register cell to be the identical Term.
    internal bool KeepsLiveRoot550(int slot, Term value)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            if (!values.TryGetValue(slot, out Term? current) || !ReferenceEquals(current, value)) return false;
            if (!owner.IsAuthenticatedLiteral801(value))
                _ = owner.IsOwnedLocal(value); // Reject any actual stale/foreign block.

            return true;
        }
    }

    internal Term Retain(Term value)
    {
        int slot = Reserve();
        Set(slot, value);

        return Read(slot);
    }

    internal void Clear(int slot)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            values.Remove(slot);
            ClearLiteralSlot801(slot);
            binaryRoots512?.Clear512(slot);
        }
    }

    public void Dispose()
    {
        lock (owner.Gate)
        {
            if (disposed)
                return;
            disposed = true;
            values.Clear();
            ReleaseRootsAndMagic576();
        }
    }
}
