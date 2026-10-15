namespace Erlang;

internal sealed class DistributionSignalDrainFlow(DistributionSignalDrain owner)
{
    internal DistributionSignalDrain Owner { get; } = owner;
    internal volatile bool Active = true;
}
