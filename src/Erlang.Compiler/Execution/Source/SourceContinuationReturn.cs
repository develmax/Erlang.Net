namespace Erlang.Compiler;

internal sealed class SourceContinuationReturn(Term value) : Exception
{
    internal Term Value { get; } = value;
}
