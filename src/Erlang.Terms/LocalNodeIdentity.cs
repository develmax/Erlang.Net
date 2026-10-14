namespace Erlang;

internal sealed class LocalNodeIdentity(string node, uint creation = 0)
{
    private LocalNodeSnapshot snapshot = new(node, creation);

    internal LocalNodeSnapshot Snapshot => Volatile.Read(ref snapshot);

    internal void Publish(string node, uint creation)
    {
        Volatile.Write(ref snapshot, new(node, creation));
    }
}
