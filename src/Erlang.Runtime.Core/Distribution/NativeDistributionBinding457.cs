namespace Erlang;

// One real native port can bind once. The row retains this captured generation
// after retirement so delayed native data cannot become ordinary mailbox input.
internal sealed class NativeDistributionBinding457(
    NativeDriverPortRegistry422 registry,
    NativeDriverPortRow422 row,
    DriverParentPeerTransport422 transport,
    RuntimeDistributionPendingEntry entry
)
{
    internal PortTerm Identity => row.Identity;
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal uint Generation { get; } = entry.ConnectionId;
    internal bool Active { get; private set; } = true;
    internal bool IsLive => Active && !row.Closing && !row.StopRequested && !row.PhysicallyStopped;
    private readonly DistributionOutputWake457 available = new();

    internal void Wake() => available.Wake();

    internal Task CaptureWakeBeforeQueueCheck() => available.CaptureBeforeQueueCheck();

    internal void Start() => registry.StartDistribution457(this, row, transport);

    internal void Accept(byte[] packet)
    {
        if (IsLive) Entry.Owner.AcceptNativePort457(this, packet);
    }

    internal void PortStopped(Term reason)
    {
        Active = false;
        Wake();
        Entry.Owner.RetireNativePort457(this, reason);
    }

    internal void Retire()
    {
        if (!Active) return;
        Active = false;
        Wake();
        registry.CloseDistribution457(this, row, transport);
    }
}
