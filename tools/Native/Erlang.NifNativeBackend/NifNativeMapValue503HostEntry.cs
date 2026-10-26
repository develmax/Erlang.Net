namespace Erlang.NifNativeBackend;

public static class NifNativeMapValue503HostEntry
{
    public static Task ShapeAndOutput() => NifNativeBinaryHostChecks.Run(NifNativeMapValue503HostChecks.ShapeAndOutput);

    public static Task OwnersAndHints() => NifNativeBinaryHostChecks.Run(NifNativeMapValue503HostChecks.OwnersAndHints);

    public static Task PendingKeys() => NifNativeBinaryHostChecks.Run(NifNativeMapValue503HostChecks.PendingKeys);

    public static Task InvalidOwnersAndThreads() => NifNativeBinaryHostChecks.Run(NifNativeMapValue503HostChecks.InvalidOwnersAndThreads);
}
