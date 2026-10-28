namespace Erlang.NifNativeBackend;

public static class NifConsCell539HostEntry
{
    public static Task ShapeAndAbi() => NifNativeBinaryHostChecks.Run(NifConsCell539HostChecks.ShapeAndAbi);

    public static Task PendingHeadAndImproperTail() => NifNativeBinaryHostChecks.Run(NifConsCell539HostChecks.PendingHeadAndImproperTail);

    public static Task InvalidOwnersAndThreads() => NifNativeBinaryHostChecks.Run(NifConsCell539HostChecks.InvalidOwnersAndThreads);
}
