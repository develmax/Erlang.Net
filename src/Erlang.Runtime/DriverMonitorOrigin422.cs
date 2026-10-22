namespace Erlang;

internal sealed class DriverMonitorOrigin422(
    ErlDriverPortIdentity420 port,
    ulong attempt,
    Pid target,
    ReferenceTerm reference
)
{
    internal ErlDriverPortIdentity420 Port { get; } = port;
    internal ulong Attempt { get; } = attempt;
    internal Pid Target { get; } = target;
    internal ReferenceTerm Reference { get; } = reference;
    internal bool Removed;
    internal bool DeathQueued;
}
