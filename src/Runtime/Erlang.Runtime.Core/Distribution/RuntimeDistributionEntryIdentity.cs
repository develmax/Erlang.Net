namespace Erlang;

internal sealed class RuntimeDistributionEntryIdentity(Atom node)
{
    internal Atom Node { get; } = node;
    internal uint Creation { get; set; }
    internal RuntimeDistributionPendingEntry? PendingEntry { get; set; }
}
