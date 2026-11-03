namespace Erlang;

internal sealed class ProcessBinaryLease512 : IDisposable
{
    private readonly ProcessBinaryRegistry512 registry;
    internal object Owner { get; }
    internal BinaryPayloadHandle512[] Handles { get; private set; }
    private bool disposed;
    private IDisposable? native524;

    internal ProcessBinaryLease512(
        ProcessBinaryRegistry512 registry,
        object owner,
        BinaryPayloadHandle512[] handles,
        IDisposable? native524 = null
    )
    {
        this.registry = registry;
        Owner = owner;
        Handles = handles;
        this.native524 = native524;
    }

    // Registry gate serializes removal and enumeration before handle retirement.
    internal void Clear512()
    {
        if (disposed)
            return;
        disposed = true;
        Array.Clear(Handles);
        Handles = [];
        Interlocked.Exchange(ref native524, null)?.Dispose();
    }

    public void Dispose() => registry.Release512(this);
}
