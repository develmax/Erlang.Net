namespace Erlang;

// An authenticated parent request identifies actual VM scheduler work. Only
// synchronous native invocation installs that role on this physical thread.
internal sealed class ErlDriverThreadRole420 : IDisposable
{
    [ThreadStatic]
    private static ErlDriverThreadRole420? current;
    private readonly ErlDriverThreadRole420? previous;
    private readonly int thread;
    private bool disposed;

    internal ErlDriverThreadRole420(bool scheduler, bool managed = true)
    {
        Scheduler = scheduler;
        Managed = managed;
        thread = Environment.CurrentManagedThreadId;
        previous = current;
        current = this;
    }

    internal bool Scheduler { get; }
    internal bool Managed { get; }
    internal static bool IsManagedThread => current?.Managed == true;
    internal static bool IsSchedulerThread => current?.Scheduler == true;

    public void Dispose()
    {
        if (disposed) return;
        if (thread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, this))
            throw new InvalidOperationException(ErlDriverThreadBridgeNames420.WrongThread);
        current = previous;
        disposed = true;
    }
}
