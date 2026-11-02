namespace Erlang;

public sealed partial class ProcessContext
{
    private readonly object groupLeaderRootsGate = new();
    private ProcessTermFrame506? groupLeaderRoots;
    private int groupLeaderSlot;
    private Pid? directGroupLeader;
    private bool groupLeaderRootsClosed;

    internal Pid GroupLeader
    {
        get
        {
            lock (groupLeaderRootsGate)
            {
                ObjectDisposedException.ThrowIf(groupLeaderRootsClosed, this);

                return groupLeaderRoots is { } roots ? (Pid)roots.Read(groupLeaderSlot)
                    : directGroupLeader ?? throw new InvalidOperationException(ProcessMetadataRootNames.InvalidGroupLeader);
            }
        }
        set
        {
            // This is deliberately a sealed PID-only path. Snapshot its actual
            // wire fields before entering the destination heap; never pass a
            // descriptor owned by another process into the local frame.
            bool direct = ReferenceEquals(value.LocalIdentity, Runtime.LocalIdentity) || Runtime.IsSharingLiteral425(value);
            bool sameHeap = !direct && ProcessTermHeap506.Storage(value) is { } storage && ReferenceEquals(storage.Owner, termHeap506);
            Pid selected = value;
            if (!direct && !sameHeap)
            {
                var snapshot = value.WireIdentity;
                selected = new Pid(snapshot.Node, value.Id, snapshot.Creation);
            }
            lock (groupLeaderRootsGate)
            {
                ObjectDisposedException.ThrowIf(groupLeaderRootsClosed, this);
                if (direct)
                {
                    groupLeaderRoots?.Dispose();
                    groupLeaderRoots = null;
                    directGroupLeader = selected;

                    return;
                }
                if (groupLeaderRoots is { } existing)
                {
                    if (sameHeap) existing.SetLocalGraph507(groupLeaderSlot, selected);
                    else existing.SetPrepared(groupLeaderSlot, selected);
                    directGroupLeader = null;

                    return;
                }
                ProcessTermFrame506 prepared = TermHeap506.Enter();
                try
                {
                    int slot = prepared.Reserve();
                    if (sameHeap) prepared.SetLocalGraph507(slot, selected);
                    else prepared.SetPrepared(slot, selected);
                    groupLeaderSlot = slot;
                    groupLeaderRoots = prepared;
                    directGroupLeader = null;
                }
                catch
                {
                    prepared.Dispose();
                    throw;
                }
            }
        }
    }

    // All escaping metadata DTOs receive an ordinary detached immutable PID;
    // an actual guest group_leader/0 read uses the live field above instead.
    internal Pid SnapshotGroupLeader()
    {
        lock (groupLeaderRootsGate)
        {
            ObjectDisposedException.ThrowIf(groupLeaderRootsClosed, this);
            Pid value = groupLeaderRoots is { } roots ? (Pid)roots.Read(groupLeaderSlot)
                : directGroupLeader ?? throw new InvalidOperationException(ProcessMetadataRootNames.InvalidGroupLeader);

            if (ReferenceEquals(value.LocalIdentity, Runtime.LocalIdentity)) return value;
            var snapshot = value.WireIdentity;

            return new Pid(snapshot.Node, value.Id, snapshot.Creation);
        }
    }

    private void CloseGroupLeaderRoots()
    {
        lock (groupLeaderRootsGate)
        {
            if (groupLeaderRootsClosed) return;
            groupLeaderRootsClosed = true;
            groupLeaderRoots?.Dispose();
            groupLeaderRoots = null;
            directGroupLeader = null;
        }
    }
}
