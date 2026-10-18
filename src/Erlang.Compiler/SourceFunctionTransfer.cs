namespace Erlang.Compiler;

internal sealed class SourceFunctionTransfer(SourceFunctionTailScope scope, SourceInvocationTarget target) : Exception
{
    internal SourceFunctionTailScope Scope { get; } = scope;
    internal SourceInvocationTarget Target { get; } = target;
}
