namespace Erlang;

internal sealed class GcStatisticsAdmission(
    ModuleRegistry registry,
    ProcessContext caller,
    PreparedBeamImage image,
    SourceCodeLease lease
) : IDisposable
{
    private ModuleRegistry? owner = registry;
    private int started;
    internal PreparedBeamImage Image { get; } = image;
    internal SourceCodeLease Lease { get; } = lease;

    internal bool TryStart(ModuleRegistry registry, ProcessContext context)
        => ReferenceEquals(Volatile.Read(ref owner), registry) && ReferenceEquals(caller, context)
            && Interlocked.CompareExchange(ref started, 1, 0) == 0;

    public void Dispose()
    {
        ModuleRegistry? registry = Interlocked.Exchange(ref owner, null);
        if (registry is null) return;
        Exception? leaseFailure = null;
        Exception? releaseFailure = null;
        try
        {
            Lease.Dispose();
        }
        catch (Exception error)
        {
            leaseFailure = error;
        }
        try
        {
            registry.ReleaseGcStatisticsUse();
        }
        catch (Exception error)
        {
            releaseFailure = error;
        }
        if (leaseFailure is not null && releaseFailure is not null)
            throw new AggregateException(leaseFailure, releaseFailure);
        if (leaseFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(leaseFailure).Throw();
        if (releaseFailure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(releaseFailure).Throw();
    }
}
