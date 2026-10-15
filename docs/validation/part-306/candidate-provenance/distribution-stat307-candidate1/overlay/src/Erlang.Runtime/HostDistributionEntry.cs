namespace Erlang;

internal sealed class HostDistributionEntry(
    string channel,
    ulong generation,
    ProcessContext controller
)
{
    internal string Channel { get; } = channel;
    internal ulong Generation { get; } = generation;
    internal ProcessContext Controller { get; } = controller;
    internal Pid InputHandler { get; set; } = controller.Self;
    internal TaskCompletionSource? CapacityChanged { get; set; }
    internal Queue<HostDistributionBuffer> Output { get; } = new();
    internal bool Closed
    {
        get; set;
    }
    internal bool GetSize
    {
        get; set;
    }
    internal bool NotificationArmed
    {
        get; set;
    }
    internal long InputPackets
    {
        get; set;
    }
    internal long OutputPackets
    {
        get; set;
    }
    internal long QueuedPayloadBytes
    {
        get; set;
    }
    internal long QueuedAccountedBytes { get; set; }

    internal int QueuedSegments
    {
        get; set;
    }
}
