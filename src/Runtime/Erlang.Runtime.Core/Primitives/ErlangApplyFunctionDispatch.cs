namespace Erlang;

internal static class ErlangApplyFunctionDispatch
{
    internal static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term[] values = Cons.Items(arguments[1]).ToArray();
        if (arguments[0] is FunctionTerm callback && callback.Arity != values.Length)
            throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(
                Term.A(ErlangErrorReasons.BadArity),
                Term.Tuple(callback, Cons.From(values))
            )));
        if (arguments[0] is FunctionTerm)
            return ListCallback.Invoke(context, arguments[0], values);
        throw Failure(context, Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), arguments[0]));
    }

    private static ErlangException Failure(ProcessContext context, Term reason)
    {
        using var frame = context.EnterGeneratedFunction(
            ErlangModuleNames.Module,
            ErlangBasicBifNames.Apply,
            ErlangApplySourceLocation.FunctionApplyArity,
            ErlangApplySourceLocation.Location,
            new object()
        );

        return context.CaptureSourceFailure(new ErlangException(reason));
    }
}
