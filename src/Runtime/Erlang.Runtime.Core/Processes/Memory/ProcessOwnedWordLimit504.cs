namespace Erlang;

internal sealed class ProcessOwnedWordLimit504(long reserved, int requested, long maximum)
    : Exception(ProcessOwnedWordNames504.Limit)
{
    internal long ReservedWords { get; } = reserved;
    internal int RequestedWords { get; } = requested;
    internal long MaximumWords { get; } = maximum;
}
