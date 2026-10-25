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
        using var frame = context.EnterBeamFunction(ErlangModuleNames.Module, ErlangBasicBifNames.Apply, ErlangApplySourceLocation.FunctionApplyArity);
        using var location = context.EnterSourceLocation(ErlangApplySourceLocation.Location);
        try
        {
            return ListCallback.Invoke(context, arguments[0], values);
        }
        catch (ErlangException error)
        {
            throw context.CaptureSourceFailure(error);
        }
    }
}
