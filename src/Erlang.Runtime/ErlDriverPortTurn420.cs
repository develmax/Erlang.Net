namespace Erlang;

internal sealed class ErlDriverPortTurn420 : IDisposable
{
    [ThreadStatic]
    private static ErlDriverPortTurn420? current;
    private readonly ErlDriverPortTurn420? previous;
    private readonly int thread;
    private readonly ErlDriverThreadRole420 role;
    private bool disposed;

    internal ErlDriverPortTurn420(nint port, nuint caller, bool schedulerThread)
    {
        Port = port;
        Caller = caller;
        SchedulerThread = schedulerThread;
        thread = Environment.CurrentManagedThreadId;
        role = new(schedulerThread);
        previous = current;
        current = this;
    }

    internal nint Port { get; }
    internal nuint Caller { get; }
    internal bool SchedulerThread { get; }
    internal static bool IsSchedulerThread => ErlDriverThreadRole420.IsSchedulerThread;

    internal static nuint CallerFor(nint port, nuint nil) => current?.Port == port && current.Caller != 0 ? current.Caller : nil;

    public void Dispose()
    {
        if (disposed) return;
        if (thread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, this))
            throw new InvalidOperationException(ErlDriverThreadBridgeNames420.WrongThread);
        role.Dispose();
        current = previous;
        disposed = true;
    }
}
