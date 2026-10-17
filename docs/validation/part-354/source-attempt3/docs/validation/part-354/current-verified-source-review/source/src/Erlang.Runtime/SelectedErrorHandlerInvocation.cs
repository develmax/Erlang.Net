namespace Erlang;

internal sealed record SelectedErrorHandlerInvocation(
    string Module,
    string Function,
    IReadOnlyList<Term> Arguments
)
{
    internal ErlangExecutionFrame? Frame { get; set; }
}
