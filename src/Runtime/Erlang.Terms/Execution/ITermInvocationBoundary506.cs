namespace Erlang;

internal interface ITermInvocationBoundary506
{
    bool OwnedInvocationEnabled506 { get; }

    ValueTask<Term> InvokeOwnedFunction506(FunctionTerm function, IReadOnlyList<Term> arguments);
}
