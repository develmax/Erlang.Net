namespace Erlang;

// The opaque ABI handle owns a real queue lock and exact source refcounts.
// Lock itself retains one reference until unlock; the port owns the initial1.
internal sealed class ErlDriverPdl420
{
    private readonly object gate = new();
    private readonly object queueGate;
    private readonly Action<ErlDriverPdl420> destroy;
    private long references = 1;
    private bool portReference = true;

    internal ErlDriverPdl420(nint identity, ErlDriverPort420 port, Action<ErlDriverPdl420> destroy)
    {
        Identity = identity;
        Port = port;
        queueGate = port.Queue.Gate;
        this.destroy = destroy;
    }

    internal nint Identity { get; }
    internal ErlDriverPort420 Port { get; }

    internal long ReferenceCount
    {
        get
        {
            lock (gate) return references;
        }
    }

    internal long Retain()
    {
        lock (gate)
        {
            if (references <= 0) throw new ObjectDisposedException(nameof(ErlDriverPdl420));
            references = checked(references + 1);

            return references;
        }
    }

    internal long Release()
    {
        long count;
        lock (gate)
        {
            if (references <= 0) throw new ObjectDisposedException(nameof(ErlDriverPdl420));
            count = --references;
        }
        if (count == 0) destroy(this);

        return count;
    }

    internal void Lock()
    {
        Retain();
        LockRetained();
    }

    internal void LockRetained()
    {
        try
        {
            Monitor.Enter(queueGate);
        }
        catch
        {
            Release();
            throw;
        }
    }

    internal void Unlock()
    {
        Monitor.Exit(queueGate);
        Release();
    }

    internal void ReleasePort()
    {
        lock (gate)
        {
            if (!portReference) return;
            portReference = false;
        }
        Release();
    }
}
