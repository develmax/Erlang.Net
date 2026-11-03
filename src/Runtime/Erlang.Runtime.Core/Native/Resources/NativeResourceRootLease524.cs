namespace Erlang;

internal sealed class NativeResourceRootLease524(NativeResourcePayload524 owner) : IDisposable
{
    // State transitions are serialized by the actual payload gate. A target
    // header may retire an unadmitted reservation before final publication.
    private NativeResourceRootLeaseState821 state;

    internal bool BelongsTo821(NativeResourcePayload524 selected) => ReferenceEquals(owner, selected);

    internal NativeResourceRootLeaseState821 State821 => state;

    internal void SetState821(NativeResourceRootLeaseState821 value) => state = value;

    internal void DisposeGuestGc536() => owner.ReleaseRootLease821(this, guestGc: true);

    internal void AdmitSuspensionAck172() => owner.AdmitReservedRoot821(this);

    public void Dispose() => owner.ReleaseRootLease821(this, guestGc: false);
}
