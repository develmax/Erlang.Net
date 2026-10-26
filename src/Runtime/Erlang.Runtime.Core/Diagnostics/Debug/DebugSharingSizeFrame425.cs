namespace Erlang;

internal sealed class DebugSharingSizeFrame425(Term value)
{
    internal Term Value { get; } = value;
    internal Term[] Children { get; } = DebugSharingGraph425.Children(value);
    internal ulong Size { get; set; } = DebugSharingWordLayout425.OwnWords(value);
    internal int Index { get; set; }
}
