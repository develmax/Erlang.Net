namespace Erlang;

public sealed class HostDistributionHandle
{
    internal HostDistributionHandle(HostDistribution owner, HostDistributionEntry entry)
    {
        Owner = owner;
        Entry = entry;
    }

    internal HostDistribution Owner { get; }
    internal HostDistributionEntry Entry { get; }
    public ulong Generation => Entry.Generation;
}
