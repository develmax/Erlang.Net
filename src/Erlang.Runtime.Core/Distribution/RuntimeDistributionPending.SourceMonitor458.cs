namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<ReferenceTerm?> TrySourceMonitor458(ProcessContext owner, Term target, ProcessMonitorOptions options)
    {
        string? node = target is Pid pid ? pid.Node
            : target is TupleTerm { Items: [Atom, Atom remote] } ? remote.Name : null;
        if (node is null || node == runtime.Node) return null;
        DistributionPublicationTarget selected;
        ValueTask<ReferenceTerm> operation;
        lock (gate)
        {
            RequireProcess(owner);
            if (!entries.TryGetValue(node, out var weak) || !weak.TryGetTarget(out var entry)
                || entry.Connected is not { } connected || !IsConnectedSignalGeneration(entry, connected, entry.ConnectionId)) return null;
            var lease = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            selected = new(
                entry,
                connected,
                entry.ConnectionId,
                lease
            );
            try
            {
                operation = runtime.BeginSourceMonitor458(
                    selected.Entry,
                    selected.Connection,
                    owner,
                    target is TupleTerm named ? named.Items[0] : target,
                    options
                );
            }
            catch
            {
                selected.Dispose();
                throw;
            }
        }
        using (selected)
            return await operation.ConfigureAwait(false);
    }
}
