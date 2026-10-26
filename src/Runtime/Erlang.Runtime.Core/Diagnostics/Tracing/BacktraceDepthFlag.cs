namespace Erlang;

internal static class BacktraceDepthFlag
{
    internal static Term Set(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[1] is not Integer depth || depth.Value < 0 || depth.Value > BacktraceDepthParameters.MaximumNativeSmall)
            throw Invalid(context, arguments);
        int value = depth.Value > BacktraceDepthParameters.MaximumDepth
            ? BacktraceDepthParameters.MaximumDepth
            : (int)depth.Value;

        return Term.I(context.Runtime.ExchangeBacktraceDepth(value));
    }

    private static ErlangException Invalid(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var information = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(BacktraceDepthNames.ErrorModule)] = Term.A(BacktraceDepthNames.ErrorFormatter)
        });

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), information, SequenceTraceNames.SystemFlag)
        };
    }
}
