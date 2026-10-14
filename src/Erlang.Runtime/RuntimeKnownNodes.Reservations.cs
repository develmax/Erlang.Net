namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    private readonly Dictionary<(string Node, uint Creation), int> reservedEpochs = new();

    internal IDisposable ReserveEpochs(IReadOnlyList<EtfNodeEpoch> epochs, LocalNodeSnapshot snapshot)
    {
        var keys = epochs.Where(epoch => !epoch.LocalMarker &&
            (epoch.Node != snapshot.Node || epoch.Creation != snapshot.Creation))
            .Select(epoch => (epoch.Node, epoch.Creation)).Distinct().ToArray();
        var reservation = new RuntimeExternalEpochReservation(() => ReleaseEpochs(keys));
        lock (gate)
        {
            foreach (var key in keys)
                _ = checked(reservedEpochs.GetValueOrDefault(key) + 1);
            foreach (var key in keys)
                reservedEpochs[key] = reservedEpochs.GetValueOrDefault(key) + 1;
        }

        return reservation;
    }

    private void ReleaseEpochs((string Node, uint Creation)[] keys)
    {
        lock (gate)
            foreach (var key in keys)
            {
                int count = reservedEpochs[key];
                if (count == 1)
                    reservedEpochs.Remove(key);
                else
                    reservedEpochs[key] = count - 1;
            }
    }
}
