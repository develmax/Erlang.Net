using System.Collections.Frozen;

namespace Erlang;

internal sealed class BinarySearchNode
{
    private readonly FrozenDictionary<byte, int> edges;
    internal bool Terminal { get; }

    internal BinarySearchNode(BinarySearchBuilderNode source)
    {
        edges = source.Edges.ToFrozenDictionary();
        Terminal = source.Terminal;
    }

    internal bool Next(byte value, out int index) => edges.TryGetValue(value, out index);
}
