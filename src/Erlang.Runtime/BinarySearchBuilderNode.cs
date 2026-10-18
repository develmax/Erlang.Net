namespace Erlang;

internal sealed class BinarySearchBuilderNode
{
    internal Dictionary<byte, int> Edges { get; } = new();
    internal bool Terminal { get; set; }
}
