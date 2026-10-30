namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    // No path holding this gate calls a heap reader or the KnownNodes gate.
    private readonly object physicalNodesGate = new();
    private readonly Dictionary<LocalNodeSnapshot, PhysicalNodeEntry> physicalNodes = new();
    private readonly TaskCompletionSource physicalNodesDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool physicalNodesClosed;
    private int physicalNodesDisposing;
    private List<Exception>? physicalNodeFailures;

    private void StopPhysicalNodeAdmission()
    {
        lock (physicalNodesGate)
        {
            physicalNodesClosed = true;
            CompletePhysicalNodeDrain();
        }
    }

    internal Task DrainPhysicalNodes()
    {
        lock (physicalNodesGate)
        {
            if (!physicalNodesClosed) throw new InvalidOperationException(PhysicalNodeEntryLayout.Invalid);
            CompletePhysicalNodeDrain();

            return physicalNodesDrained.Task;
        }
    }

    private void CompletePhysicalNodeDrain()
    {
        if (!physicalNodesClosed || physicalNodes.Count != 0 || physicalNodesDisposing != 0) return;
        if (physicalNodeFailures is { Count: > 0 }) physicalNodesDrained.TrySetException(physicalNodeFailures);
        else physicalNodesDrained.TrySetResult();
    }

    private bool HasPhysicalNodeEpoch(string node, uint creation)
    {
        lock (physicalNodesGate) return physicalNodes.ContainsKey(new(node, creation));
    }

    // Genuine preparation, outside heap/runtime/mailbox locks. Native allocation
    // is admitted under this isolated gate, before close can publish completion.
    internal PhysicalNodeLease AcquirePhysicalNode(LocalNodeSnapshot snapshot)
    {
        lock (physicalNodesGate)
        {
            ObjectDisposedException.ThrowIf(physicalNodesClosed, this);
            if (physicalNodes.TryGetValue(snapshot, out var entry))
            {
                var lease = new PhysicalNodeLease(this, entry);
                entry.References = checked(entry.References + 1);

                return lease;
            }
            var created = new PhysicalNodeEntry(snapshot);
            try
            {
                var first = new PhysicalNodeLease(this, created);
                physicalNodes.Add(snapshot, created);
                created.References = 1;

                return first;
            }
            catch
            {
                created.Dispose();
                throw;
            }
        }
    }

    internal PhysicalNodeLease RetainPhysicalNode(PhysicalNodeLease source)
    {
        var lease = new PhysicalNodeLease(this, source.Entry);
        lock (physicalNodesGate)
        {
            _ = source.Read();
            LocalNodeSnapshot snapshot = source.Entry.Read();
            if (!physicalNodes.TryGetValue(snapshot, out var actual) || !ReferenceEquals(actual, source.Entry))
                throw new InvalidOperationException(PhysicalNodeEntryLayout.Invalid);
            actual.References = checked(actual.References + 1);
        }

        return lease;
    }

    internal void ReleasePhysicalNode(PhysicalNodeEntry entry)
    {
        lock (physicalNodesGate)
        {
            LocalNodeSnapshot snapshot = entry.Read();
            if (!physicalNodes.TryGetValue(snapshot, out var actual) || !ReferenceEquals(actual, entry)
                || entry.References < 1) throw new InvalidOperationException(PhysicalNodeEntryLayout.Invalid);
            if (entry.References > 1)
            {
                entry.References--;

                return;
            }
            physicalNodesDisposing = checked(physicalNodesDisposing + 1);
            physicalNodes.Remove(snapshot);
            entry.References = 0;
        }
        Exception? failure = null;
        try
        {
            entry.Dispose();
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            lock (physicalNodesGate)
            {
                if (failure is not null) (physicalNodeFailures ??= new()).Add(failure);
                physicalNodesDisposing--;
                CompletePhysicalNodeDrain();
            }
        }
    }
}
