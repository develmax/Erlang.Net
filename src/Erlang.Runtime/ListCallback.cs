namespace Erlang;

internal static class ListCallback
{
    public static ValueTask<Term> Invoke(ProcessContext context, Term function, IReadOnlyList<Term> arguments)
    {
        if (function is not FunctionTerm callback)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), function));

        return callback.Invoke(context, arguments);
    }

    public static async ValueTask<bool> Compare(
        ProcessContext context,
        Term function,
        Term left,
        Term right
    )
    {
        await context.ReduceAsync();
        Term result = await Invoke(context, function, [left, right]);
        if (result is Atom { Name: ErlangBooleanAtoms.True })
            return true;
        if (result is Atom { Name: ErlangBooleanAtoms.False })
            return false;

        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));
    }
}
