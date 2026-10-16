namespace Erlang;

internal sealed class HostDistributionStagedOutput(
    HostDistribution owner,
    HostDistributionHandle handle,
    HostDistributionBuffer packet
) : IDisposable
{
    internal HostDistribution Owner { get; } = owner;
    internal HostDistributionHandle Handle { get; } = handle;
    internal HostDistributionBuffer Packet { get; } = packet;
    internal bool Active { get; set; } = true;

    public void Dispose() => Owner.ReleaseStagedOutput(this);
}
