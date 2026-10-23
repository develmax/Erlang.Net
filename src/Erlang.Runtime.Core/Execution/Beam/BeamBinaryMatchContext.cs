namespace Erlang;

internal sealed class BeamBinaryMatchContext(BitString source)
{
    internal BitString Source { get; } = source;
    internal int Position { get; set; }
}
