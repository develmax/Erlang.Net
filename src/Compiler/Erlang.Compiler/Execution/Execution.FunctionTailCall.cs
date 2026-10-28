namespace Erlang.Compiler;

public static partial class Execution
{
    private sealed class FunctionTailCall(FunctionTerm function, IReadOnlyList<Term> arguments) : Exception, IOwnedControlTransfer506
    {
        internal FunctionTerm Function { get; } = function;
        internal IReadOnlyList<Term> Arguments { get; } = arguments;

        Exception IOwnedControlTransfer506.TransferTerms506(Func<Term, Term> publish)
        {
            var payload = (TupleTerm)publish(Term.Tuple(Function, Term.Tuple(Arguments.ToArray())));

            return new FunctionTailCall((FunctionTerm)payload.Items[0], ((TupleTerm)payload.Items[1]).Items.ToArray());
        }
    }
}
