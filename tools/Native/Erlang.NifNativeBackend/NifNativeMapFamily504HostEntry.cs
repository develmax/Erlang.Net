namespace Erlang.NifNativeBackend;

public static class NifNativeMapFamily504HostEntry
{
    public static Task MutationAndOutput() => NifNativeBinaryHostChecks.Run(NifNativeMapFamily504HostChecks.MutationAndOutput);

    public static Task ArraysAndAliases() => NifNativeBinaryHostChecks.Run(NifNativeMapFamily504HostChecks.ArraysAndAliases);

    public static Task PendingValuesAndReplacement() => NifNativeBinaryHostChecks.Run(NifNativeMapFamily504HostChecks.PendingValuesAndReplacement);

    public static Task CopyBoundariesAndOwnerLifetime() => NifNativeBinaryHostChecks.Run(NifNativeMapFamily504HostChecks.CopyBoundariesAndOwnerLifetime);

    public static Task InvalidOwnersAndThreads() => NifNativeBinaryHostChecks.Run(NifNativeMapFamily504HostChecks.InvalidOwnersAndThreads);
}
