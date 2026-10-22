namespace Erlang;

internal sealed record ProcessTraceInvocation(Term Mfa, bool ReturnTrace, bool ExceptionTrace)
{
    internal IReadOnlyList<ProcessTraceChildInvocation425>? Children425 { get; init; }
}
