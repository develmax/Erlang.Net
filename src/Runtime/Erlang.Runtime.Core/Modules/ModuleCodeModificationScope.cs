namespace Erlang;

internal sealed class ModuleCodeModificationScope : IDisposable
{
    internal readonly ModuleCodeModificationPermission Owner;
    internal readonly ModuleCodeModificationScope? Previous;
    internal readonly bool ReleasesPermission;
    internal readonly int OwningThread = Environment.CurrentManagedThreadId;
    private bool disposed;

    internal ModuleCodeModificationScope(
        ModuleCodeModificationPermission owner,
        ModuleCodeModificationScope? previous,
        bool releasesPermission
    )
    {
        Owner = owner;
        Previous = previous;
        ReleasesPermission = releasesPermission;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        Owner.Exit(this);
        disposed = true;
    }
}
