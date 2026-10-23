namespace Erlang;

internal static class ErlangIntegerParseBridge
{
    internal static Term Parse(ProcessContext context, IReadOnlyList<Term> arguments, bool binary)
    {
        string function = binary ? ErlangDataBifNames.BinaryToInteger : ErlangDataBifNames.ListToInteger;
        using var source = context.EnterSourceTailCall(ErlangModuleNames.Module, function);
        try
        {
            int radix = ErlangIntegerText.Base(arguments);

            return binary
                ? ErlangIntegerText.Parse(
                    ErlangIoData.Binary(arguments[0]).Select(value => (int)value),
                    radix
                )
                : ErlangIntegerListParse.Parse(arguments[0], radix);
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                ErlangModuleNames.Module,
                function,
                arguments
            );
        }
    }
}
