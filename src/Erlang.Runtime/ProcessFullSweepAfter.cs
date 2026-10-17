namespace Erlang;

internal static class ProcessFullSweepAfter
{
    internal static ushort ParseProcess(Term value)
    {
        if (value is not Integer number || number.Value < 0 || number.Value > ProcessFullSweepNames.MaximumNativeSmall)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        // Process/spawn paths assign to native Uint16, unlike system_flag's explicit clamp.
        return unchecked((ushort)(ulong)number.Value);
    }

    internal static Term SetSelf(ProcessContext context, Term value)
        => Term.I(context.GarbageCollectionPolicy.ExchangeThreshold(ParseProcess(value)));

    internal static Term SetDefault(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[1] is not Integer number || number.Value < 0 || number.Value > ProcessFullSweepNames.MaximumNativeSmall)
            throw Invalid(context, arguments);
        ushort threshold = number.Value > ushort.MaxValue ? ushort.MaxValue : (ushort)number.Value;

        return Term.I(context.Runtime.ExchangeDefaultFullSweepAfter(threshold));
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
