namespace Erlang;

internal sealed class ProcessTermFrame506 : IDisposable
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
        values.Add(0, value);
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
                values[slot] = value;

                return;
            }
        }
        using PreparedTermTransfer506 prepared = owner.Prepare(this, value);
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            values[slot] = owner.ImportPrepared(prepared.Portable);
        }
    }

    internal void SetPrepared(int slot, Term portable)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            values[slot] = owner.ImportPrepared(portable);
        }
    }

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
            owner.Release(this);
        }
    }
}
