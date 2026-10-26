using System.Numerics;

namespace Erlang;

internal sealed class RuntimeNodeRegistration458(ProcessContext owner, RuntimeDistributionPendingEntry entry)
{
    internal ProcessContext Owner { get; } = owner;
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal uint Generation { get; } = entry.ConnectionId;
    internal BigInteger Count { get; set; } = BigInteger.One;
    internal bool Retired { get; set; }
}
