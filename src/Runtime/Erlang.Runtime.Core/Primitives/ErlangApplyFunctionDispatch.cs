namespace Erlang;

internal static class ErlangApplyFunctionDispatch
{
    internal static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term[] values = Cons.Items(arguments[1]).ToArray();
        if (arguments[0] is FunctionTerm callback && callback.Arity != values.Length)
            throw ErlangFunctionCallErrors.Capture(context, Term.Tuple(
                Term.A(ErlangErrorReasons.BadArity),
                Term.Tuple(callback, Cons.From(values))
            ));
        if (arguments[0] is FunctionTerm)
            return ListCallback.Invoke(context, arguments[0], values);
        throw ErlangFunctionCallErrors.Capture(context, Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), arguments[0]));
    }
}
