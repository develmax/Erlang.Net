namespace Erlang;

internal interface IOwnedFunctionStorage : IOwnedTermSequence506
{
    int ReadFunctionArity();

    IReadOnlyList<Term> ReadFunctionEnvironment();

    ValueTask<Term> InvokeFunctionBody(ITermExecutionContext context, IReadOnlyList<Term> arguments);
}
