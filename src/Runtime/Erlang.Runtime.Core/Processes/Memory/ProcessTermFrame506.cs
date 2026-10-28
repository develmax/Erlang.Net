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
        using PreparedMapGraph517 maps = owner.PrepareMapGraph517(prepared.Portable, false);
        using PreparedMapGraphUse517 admitted = maps.AdmitBuilds517();
        using ProcessTermFrame506 construction = owner.Enter();
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            ProcessTermConstruction514 custody = construction.BeginConstruction514(prepared.Portable);
            PublishRoot512(slot, owner.ImportPrepared(
                prepared.Portable,
                maps,
                custody,
                admitted
            ));
        }
    }

    internal void SetPrepared(int slot, Term portable)
    {
        using ProcessTermBorrow506 borrow = owner.Borrow(this, portable);
        using PreparedMapGraph517 maps = owner.PrepareMapGraph517(portable, false);
        using PreparedMapGraphUse517 admitted = maps.AdmitBuilds517();
        using ProcessTermFrame506 construction = owner.Enter();
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            ProcessTermConstruction514 custody = construction.BeginConstruction514(portable);
            PublishRoot512(slot, owner.ImportPrepared(
                portable,
                maps,
                custody,
                admitted
            ));
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
