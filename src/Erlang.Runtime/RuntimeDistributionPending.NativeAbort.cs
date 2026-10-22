namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    // Native abort's boolean is "not connected", not "was pending".
    // Keep the existing public AbortPending(handle) API/generation checks intact.
    internal bool AbortPendingConnectionNative(Atom remoteNode, Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                if (entry.Node.Name != remoteNode.Name || remoteNode.Name == runtime.Node)
                    throw new ArgumentException(DistributionPendingDiagnostics.Node, nameof(remoteNode));
                // Source checks identity first, but a stale generation is a
                // successful no-op and must not retire a newer pending entry.
                if (entry.ConnectionId != connectionId) return true;
                if (entry.State == DistributionPendingState.Connected) return false;
                if (entry.State == DistributionPendingState.Pending) RetirePending(entry);

                return true;
            }
        }
    }
}
