namespace Erlang.NifNativeBackend;

public static class NifNativeMapCreation501HostEntry
{
    public static Task CreationAndOwnership() => NifNativeBinaryHostChecks.Run(NifNativeMapCreation501HostChecks.CreationAndOwnership);

    public static Task InvalidEnvironments() => NifNativeBinaryHostChecks.Run(NifNativeMapCreation501HostChecks.InvalidEnvironments);

    public static Task ThreadAccess() => NifNativeBinaryHostChecks.Run(NifNativeMapCreation501HostChecks.ThreadAccess);
}
