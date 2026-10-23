namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal IReadOnlyList<(Atom Node, uint? ConnectionId, bool? Visible)> QueryNodeSnapshot()
    {
        lock (gate)
        {
            var result = new List<(Atom Node, uint? ConnectionId, bool? Visible)>();
            foreach (var weak in entries.Values)
            {
                if (!weak.TryGetTarget(out var entry) || entry.Node.Name == runtime.Node)
                    continue;
                if (entry.Connected is { } connected && IsConnectedSignalGeneration(entry, connected, entry.ConnectionId))
                    result.Add((entry.Node, entry.ConnectionId, (connected.Flags & DistributionConnectionFlags.Published) != 0));
                else
                    result.Add((entry.Node, null, null));
            }

            return result;
        }
    }
}
