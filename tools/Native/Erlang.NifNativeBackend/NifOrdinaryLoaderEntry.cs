namespace Erlang.NifNativeBackend;

public static class NifOrdinaryLoaderEntry
{
    public static Task Run(string[] arguments) => NifOrdinaryLoaderDriver.Run(arguments);
}
