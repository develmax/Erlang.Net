namespace Erlang;

internal sealed class DebugSharingSizeFrame425(DebugSharingSizeGraph528 graph, Term value)
{
    internal Term Value { get; } = value;
    internal Term[] Children { get; } = graph.Children(value);
    internal ulong Size { get; set; } = graph.OwnWords(value, shared: false);
    internal int Index { get; set; }
}
