namespace Erlang;

internal sealed class NativeDistChannelRequest429(
    RuntimeDistributionPendingEntry entry,
    ProcessContext controller,
    ProcessContext kernel,
    Pid setup,
    ReferenceTerm reply,
    ResourceReferenceTerm handle,
    ulong flags,
    uint creation
)
{
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal uint Generation { get; } = entry.ConnectionId;
    internal ProcessContext Controller { get; } = controller;
    internal ProcessContext Kernel { get; } = kernel;
    internal Pid Setup { get; } = setup;
    internal ReferenceTerm Reply { get; } = reply;
    internal ResourceReferenceTerm Handle { get; } = handle;
    internal ulong Flags { get; } = flags;
    internal uint Creation { get; } = creation;
}
