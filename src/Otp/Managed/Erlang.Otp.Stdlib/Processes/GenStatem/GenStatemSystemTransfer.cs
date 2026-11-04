namespace Erlang;

// Publish the current sys request while all machine/sys invocation scopes unwind.
internal sealed class GenStatemSystemTransfer(ProcessContext context, IReadOnlyList<Term> arguments)
    : Exception, IOwnedControlTransfer506
{
    internal ProcessContext Context { get; } = context;
    internal IReadOnlyList<Term> Arguments { get; } = arguments;

    Exception IOwnedControlTransfer506.TransferTerms506(Func<Term, Term> publish)
        => new GenStatemSystemTransfer(Context, ((TupleTerm)publish(Term.Tuple(Arguments.ToArray()))).Items);
}
