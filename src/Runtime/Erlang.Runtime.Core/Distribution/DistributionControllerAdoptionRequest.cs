namespace Erlang;

internal sealed class DistributionControllerAdoptionRequest(
    RuntimeDistributionPendingEntry entry,
    ProcessContext controller,
    ProcessContext kernel,
    Pid setupPid,
    ReferenceTerm replyId,
    ulong flags,
    uint creation
)
{
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal ProcessContext Controller { get; } = controller;
    internal ProcessContext Kernel { get; } = kernel;
    internal Pid SetupPid { get; } = setupPid;
    internal ReferenceTerm ReplyId { get; } = replyId;
    internal ulong Flags { get; } = flags;
    internal uint Creation { get; } = creation;
    internal uint ConnectionId { get; } = entry.ConnectionId;
}
