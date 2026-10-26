namespace Erlang.NifNativeBackend;

public static class NifTextBinaryCallbackEntry
{
    public static Task Run(string[] arguments) => NifTextBinaryCallbackDriver.Run(arguments);
}
