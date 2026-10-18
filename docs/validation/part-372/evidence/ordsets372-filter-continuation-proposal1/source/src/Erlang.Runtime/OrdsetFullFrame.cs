namespace Erlang;

internal sealed record OrdsetFullFrame(
    string Module,
    string Function,
    int Arity,
    int Line
)
{
    internal OrdsetFullContinuationKind ContinuationKind { get; init; }
}
