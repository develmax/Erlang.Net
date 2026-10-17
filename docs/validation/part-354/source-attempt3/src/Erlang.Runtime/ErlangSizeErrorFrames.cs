namespace Erlang;

internal static class ErlangSizeErrorFrames
{
    internal static ErlangException Invalid(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason
    )
    {
        var information = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(ErlangSizeErrorNames.Module)] = Term.A(ErlangSizeErrorNames.Formatter)
        });

        return new ErlangException(reason)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), information, function)
        };
    }
}
