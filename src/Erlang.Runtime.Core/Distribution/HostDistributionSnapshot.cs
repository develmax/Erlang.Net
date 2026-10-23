namespace Erlang;

public sealed record HostDistributionSnapshot(
    ulong Generation,
    Pid Controller,
    Pid InputHandler,
    bool GetSize,
    bool NotificationArmed,
    long AcceptedInputPackets,
    long ConsumedOutputPackets,
    long QueuedPayloadBytes,
    int QueuedBuffers,
    int QueuedSegments
);
