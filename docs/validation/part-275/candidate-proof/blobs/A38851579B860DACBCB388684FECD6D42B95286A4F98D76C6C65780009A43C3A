namespace Erlang;

public sealed record DistributionConnectedSnapshot(
    Atom Node,
    uint ConnectionId,
    ulong Flags,
    uint RemoteCreation,
    Pid Controller,
    Pid? InputHandler,
    bool GetSize,
    bool NotificationArmed,
    long AcceptedInputPackets,
    long ConsumedOutputPackets,
    long QueuedPacketBytes,
    int QueuedPackets,
    int QueuedSegments
);
