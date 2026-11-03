namespace Erlang;

internal sealed class ProcessBinaryLease512 : IDisposable
{
    private readonly ProcessBinaryRegistry512 registry;
    internal object Owner { get; }
    internal BinaryPayloadHandle512[] Handles { get; private set; }
    // Only the registry-selected active summary owner has this metadata.
    // The registry clears it under its gate before removal/handle retirement.
    internal Term? CachedRoot466 { get; set; }
    // Intrusive eligible-active ring; no extra backing or native ownership.
    internal Term? SummaryRoot495 { get; set; }
    internal ProcessBinaryLease512? SummaryPrevious495 { get; set; }
    internal ProcessBinaryLease512? SummaryNext495 { get; set; }
    internal bool HasAdditionalRoots495 => native524 is not null;
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
