namespace Erlang;

internal sealed class ModuleCodeModificationTicket : IDisposable
{
    private readonly object gate = new();
    private readonly ModuleCodeModificationPermission owner;
    private bool released;
    private bool disposeRequested;
    private int activeRuns;

    internal ModuleCodeModificationTicket(ModuleCodeModificationPermission owner) => this.owner = owner;

    internal bool IsActiveFor(ModuleCodeModificationPermission permission)
    {
        lock (gate)
            return !released && ReferenceEquals(owner, permission);
    }

    internal T Run<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        lock (gate)
        {
            if (disposeRequested)
                throw new ObjectDisposedException(nameof(ModuleCodeModificationTicket), ModuleCodeModificationNames.ReleasedTicket);
            activeRuns++;
            try
            {
                using var scope = owner.EnterOwned();

                return operation();
            }
            finally
            {
                activeRuns--;
                ReleaseWhenIdle();
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposeRequested = true;
            ReleaseWhenIdle();
        }
    }

    private void ReleaseWhenIdle()
    {
        if (disposeRequested && !released && activeRuns == 0)
        {
            released = true;
            owner.Release();
        }
    }
}
