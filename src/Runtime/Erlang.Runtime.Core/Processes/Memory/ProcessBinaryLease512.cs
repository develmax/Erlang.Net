namespace Erlang;

internal sealed class ProcessBinaryLease512 : IDisposable
{
    private readonly ProcessBinaryRegistry512 registry;
    internal object Owner { get; }
    internal BinaryPayloadHandle512[] Handles { get; private set; }
    private bool disposed;

    internal ProcessBinaryLease512(ProcessBinaryRegistry512 registry, object owner, BinaryPayloadHandle512[] handles)
    {
        this.registry = registry;
        Owner = owner;
        Handles = handles;
    }

    // Registry gate serializes removal and enumeration before handle retirement.
    internal void Clear512()
    {
        if (disposed)
            return;
        disposed = true;
        Array.Clear(Handles);
        Handles = [];
    }

    public void Dispose() => registry.Release512(this);
}
