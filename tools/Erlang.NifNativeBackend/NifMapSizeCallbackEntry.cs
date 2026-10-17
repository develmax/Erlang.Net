namespace Erlang.NifNativeBackend;

public static class NifMapSizeCallbackEntry
{
    public static Task Run(string[] arguments) => NifMapSizeCallbackDriver.Run(arguments);
}
