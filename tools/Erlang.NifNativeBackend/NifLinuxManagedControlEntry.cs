namespace Erlang.NifNativeBackend;

public static class NifLinuxManagedControlEntry
{
    public static Task Run(string[] arguments) => NifLinuxManagedControlDriver.Run(arguments);
}
