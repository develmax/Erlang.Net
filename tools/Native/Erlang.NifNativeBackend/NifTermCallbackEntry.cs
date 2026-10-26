namespace Erlang.NifNativeBackend;

public static class NifTermCallbackEntry
{
    public static Task Run(string[] arguments) => NifTermCallbackDriver.Run(arguments);
}
