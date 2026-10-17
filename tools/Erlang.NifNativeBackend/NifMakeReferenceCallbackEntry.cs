namespace Erlang.NifNativeBackend;

public static class NifMakeReferenceCallbackEntry
{
    public static Task Run(string[] arguments) => NifMakeReferenceCallbackDriver.Run(arguments);
}
