namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        lock (gate)
        {
            foreach (var entry in entries.Values.Select(weak => weak.TryGetTarget(out var entry) ? entry : null)
                .OfType<RuntimeDistributionPendingEntry>().Concat(pending.Values).Concat(controllers.Values).Distinct())
                if (entry.Connected is { } connected)
                    snapshot.Distribution(
                        entry.Node.Name,
                        NodeDistReferenceSnapshot.Owner(
                            connected.ControllerIdentity is Pid ? NodeDistReferenceNames.Process : NodeDistReferenceNames.Port,
                            connected.ControllerIdentity
                        ),
                        NodeDistReferenceNames.Control
                    );
        }
    }
}
