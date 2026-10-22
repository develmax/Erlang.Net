namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    // An external DTO may name today's local epoch, then become external after
    // a real node transition. Do not apply the import-local-marker filter here.
    internal IDisposable ReservePersistentEpoch416(string node, uint creation)
    {
        var keys = new[] { (Node: node, Creation: creation) };
        var weakOwner = new WeakReference<RuntimeKnownNodes>(this);
        var result = new RuntimeExternalEpochReservation(() =>
        {
            if (weakOwner.TryGetTarget(out var owner)) owner.ReleaseEpochs(keys);
        });
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(observedIdentitiesClosed, this);
            int count = checked(reservedEpochs.GetValueOrDefault(keys[0]) + 1);
            reservedEpochs[keys[0]] = count;
        }

        return result;
    }
}
