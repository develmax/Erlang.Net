namespace Erlang;

internal static class ErlangIntegerParseBridge
{
    internal static Term Parse(ProcessContext context, IReadOnlyList<Term> arguments, bool binary)
    {
        string function = binary ? ErlangDataBifNames.BinaryToInteger : ErlangDataBifNames.ListToInteger;
        using var source = context.EnterSourceTailCall(ErlangModuleNames.Module, function);
        bool big = false;
        try
        {
            int radix = ErlangIntegerText.Base(arguments);

            return binary
                ? ErlangIntegerBinaryParse.Parse(arguments[0], radix, out big)
                : ErlangIntegerListParse.Parse(arguments[0], radix, out big);
        }
        catch (ErlangException error)
        {
            throw new ErlangException(error.Reason, error.ExceptionClass)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(
                    Cons.From(arguments),
                    ErlangErrorHelperMetadata.Info,
                    function,
                    big ? PortErrorSourceLocations.ErrorWithInfo : ErlangErrorHelperMetadata.BadArgumentWithInfo
                )
            };
        }
    }
}
