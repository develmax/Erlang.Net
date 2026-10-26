namespace Erlang;

public sealed record HostDistributionSnapshot(
    ulong Generation,
    Term Controller,
    Term InputHandler,
    bool GetSize,
    bool NotificationArmed,
    long AcceptedInputPackets,
    long ConsumedOutputPackets,
    long QueuedPayloadBytes,
    int QueuedBuffers,
    int QueuedSegments
);
