namespace Erlang.NifNativeBackend;

public static class NifLinuxCallbackEntry
{
    public static Task Run(string[] arguments) => NifLinuxCallbackDriver.Run(arguments);
}
