namespace Erlang;

internal sealed class StatisticsTrapAdmission(ModuleRegistry registry, PreparedBeamImage image) : IDisposable
{
    private ModuleRegistry? owner = registry;
    internal PreparedBeamImage Image { get; } = image;

    internal bool IsCurrentFor(ModuleRegistry registry) => ReferenceEquals(Volatile.Read(ref owner), registry);

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.ReleaseStatisticsTrapUse();
}
