namespace Erlang;

internal sealed class RuntimeMapHashEpoch(ulong word)
{
    internal ulong Word { get; } = word;
    internal int ActiveScopes { get; set; }
}
