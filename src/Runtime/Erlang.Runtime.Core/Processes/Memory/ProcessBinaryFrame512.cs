namespace Erlang;

internal sealed class ProcessBinaryFrame512(ProcessBinaryRegistry512 registry, object owner) : IDisposable
{
    private readonly Dictionary<int, ProcessBinaryLease512> roots = new();
    private bool disposed;

    internal ProcessBinaryPublication512 Prepare512(int slot, Term value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ProcessBinaryLease512 next = registry.Retain512(owner, value);
        try
        {
            roots.TryGetValue(slot, out ProcessBinaryLease512? previous);
            var publication = new ProcessBinaryPublication512(
                this,
                slot,
                previous,
                next
            );
            roots[slot] = next;

            return publication;
        }
        catch
        {
            next.Dispose();
            throw;
        }
    }

    internal void Rollback512(int slot, ProcessBinaryLease512? previous)
    {
        if (previous is null)
            roots.Remove(slot);
        else
            roots[slot] = previous;
    }

    internal void Clear512(int slot)
    {
        if (roots.Remove(slot, out ProcessBinaryLease512? previous))
            previous.Dispose();
    }

    internal void ClearAll512()
    {
        foreach (ProcessBinaryLease512 lease in roots.Values)
            lease.Dispose();
        roots.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        ClearAll512();
        registry.Leave512(owner);
    }
}
