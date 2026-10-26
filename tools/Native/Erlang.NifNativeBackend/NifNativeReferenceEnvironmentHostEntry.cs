namespace Erlang.NifNativeBackend;

public static class NifNativeReferenceEnvironmentHostEntry
{
    public static Task Lifetime() => NifNativeBinaryHostChecks.Run(NifNativeReferenceEnvironmentHostChecks.Lifetime);

    public static Task ForeignThreads() => NifNativeBinaryHostChecks.Run(NifNativeReferenceEnvironmentHostChecks.ForeignThreads);
}
