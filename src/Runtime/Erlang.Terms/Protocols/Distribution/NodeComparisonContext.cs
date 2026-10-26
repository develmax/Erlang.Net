namespace Erlang;

internal struct NodeComparisonContext
{
    private LocalNodeIdentity? firstIdentity;
    private LocalNodeSnapshot? firstSnapshot;
    private Dictionary<LocalNodeIdentity, LocalNodeSnapshot>? additionalSnapshots;

    internal LocalNodeSnapshot Read(LocalNodeIdentity? identity, LocalNodeSnapshot current)
    {
        if (identity is null)
            return current;
        if (firstIdentity is null)
        {
            firstIdentity = identity;
            firstSnapshot = current;

            return current;
        }
        if (ReferenceEquals(firstIdentity, identity))
            return firstSnapshot!;
        additionalSnapshots ??= new();
        if (!additionalSnapshots.TryGetValue(identity, out var snapshot))
        {
            snapshot = current;
            additionalSnapshots.Add(identity, snapshot);
        }

        return snapshot;
    }
}
