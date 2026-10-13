namespace Erlang;

internal sealed class SequenceTraceState
{
    public SequenceTraceToken? Token { get; set; }
    public ulong Clock { get; set; }
    public ulong LastCount { get; set; }
}
