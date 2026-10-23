namespace Erlang;

internal sealed class ErlDriverPortTurn420 : IDisposable
{
    [ThreadStatic]
    private static ErlDriverPortTurn420? current;
    private readonly ErlDriverPortTurn420? previous;
    private readonly int thread;
    private readonly ErlDriverThreadRole420 role;
    private bool disposed;
    private List<ErlDriverPort420>? children455;

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

    private static ErlDriverPortTurn420? CreatorTurn455(nint creator)
    {
        ErlDriverPortTurn420? owner = null;
        for (var turn = current; turn is not null; turn = turn.previous)
            if (!turn.disposed && turn.SchedulerThread && turn.thread == Environment.CurrentManagedThreadId
                && (turn.Port == creator || turn.children455?.Any(child => child.NativePort == creator) == true))
                owner = turn;

        return owner;
    }

    internal static bool IsCreatingTurn455(nint creator) => CreatorTurn455(creator) is not null;

    internal static void RegisterChild455(nint creator, ErlDriverPort420 child)
    {
        var turn = CreatorTurn455(creator) ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        (turn.children455 ??= new()).Add(child);
    }

    internal void ReleaseChildren455()
    {
        var owned = children455;
        children455 = null;
        if (owned is null) return;
        List<Exception>? failures = null;
        for (int index = owned.Count - 1; index >= 0; index--)
            try
            {
                owned[index].ReleaseConstruction455();
            }
            catch (Exception failure)
            {
                (failures ??= new()).Add(failure);
            }
        if (failures is not null) throw new AggregateException(failures);
    }

    internal static nuint CallerFor(nint port, nuint nil) => current?.Port == port && current.Caller != 0 ? current.Caller : nil;

    public void Dispose()
    {
        if (disposed) return;
        if (thread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, this))
            throw new InvalidOperationException(ErlDriverThreadBridgeNames420.WrongThread);
        try
        {
            ReleaseChildren455();
        }
        finally
        {
            try
            {
                role.Dispose();
            }
            finally
            {
                current = previous;
                disposed = true;
            }
        }
    }
}
