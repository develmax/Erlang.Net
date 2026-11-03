namespace Erlang;

// The address index authenticates native ABI pointers. Membership is not a root:
// only explicit native/guest/store leases control physical retirement.
internal static class NativeResourceAddresses524
{
    private static readonly object gate = new();
    private static readonly Dictionary<nint, NativeResourcePayload524> live = new();
    private static int count;
    internal static bool AnyLive524 => Volatile.Read(ref count) != 0;

    internal static void Register524(NativeResourcePayload524 payload)
    {
        lock (gate)
        {
            live.Add(payload.IdentityPointer524, payload);
            Volatile.Write(ref count, live.Count);
        }
    }

    internal static NativeResourcePayload524 Resolve524(nint address)
    {
        lock (gate)
            return live.TryGetValue(address, out var payload) ? payload
                : throw new ArgumentException(NativeResource524Names.Retired);
    }

    internal static void Forget524(NativeResourcePayload524 payload)
    {
        lock (gate)
        {
            if (live.TryGetValue(payload.IdentityPointer524, out var current) && ReferenceEquals(current, payload))
                live.Remove(payload.IdentityPointer524);
            Volatile.Write(ref count, live.Count);
        }
    }
}
