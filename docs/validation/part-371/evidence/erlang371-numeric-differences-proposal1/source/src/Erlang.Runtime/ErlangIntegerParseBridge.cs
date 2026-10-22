namespace Erlang;

internal static class ErlangIntegerParseBridge
{
    internal static Term Parse(ProcessContext context, IReadOnlyList<Term> arguments, bool binary)
    {
        string function = binary ? ErlangDataBifNames.BinaryToInteger : ErlangDataBifNames.ListToInteger;
        using var source = context.EnterSourceTailCall(ErlangModuleNames.Module, function);
        try
        {
            return ErlangIntegerText.Parse(
                binary ? ErlangIoData.Binary(arguments[0]).Select(value => (int)value) : ErlangIntegerText.ListCharacters(arguments[0]),
                ErlangIntegerText.Base(arguments)
            );
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
