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

    internal void Set(int slot, Term value)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            if (owner.IsOwnedLocal(value))
            {
                PublishRoot512(slot, value);

                return;
            }
        }
        using PreparedTermTransfer506 prepared = owner.Prepare(this, value);
        using PreparedHeapImport imported = owner.PrepareHeapImport(this, prepared.Portable);
        SetLocalGraph507(slot, imported.Value);
    }

    internal void SetPrepared(int slot, Term portable)
    {
        lock (owner.Gate) ValidateSlot(slot);
        using PreparedHeapImport imported = owner.PrepareHeapImport(this, portable);
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
