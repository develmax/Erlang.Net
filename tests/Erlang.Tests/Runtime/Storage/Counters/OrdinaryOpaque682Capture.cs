namespace Erlang.Tests;

internal sealed class OrdinaryOpaque682Capture
{
    internal Term? Received { get; private set; }
    internal ResourceReferenceTerm? Reference => Received as ResourceReferenceTerm;
    internal int Calls { get; private set; }

    internal ValueTask<Term> Store(ITermExecutionContext context, IReadOnlyList<Term> arguments)
    {
        if (context is not ProcessContext || arguments.Count != 1)
            throw new InvalidOperationException(OrdinaryOpaque682Names.Failure);
        Received = arguments[0];
        Calls++;

        return ValueTask.FromResult<Term>(Term.A(OrdinaryOpaque682Names.Ok));
    }
}
