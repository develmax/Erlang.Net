namespace Erlang.Compiler;

internal sealed class RuntimeTailCall(
    string module,
    string function,
    Term[] arguments
) : Exception, IOwnedControlTransfer506
{
    public string Module { get; } = module;
    public string Function { get; } = function;
    public Term[] Arguments { get; } = arguments;

    Exception IOwnedControlTransfer506.TransferTerms506(Func<Term, Term> publish)
        => new RuntimeTailCall(Module, Function, SourceContinuationPayload506.Arguments(Arguments, publish));
}
