namespace Erlang.NifNativeBackend;

public static class NifNativeLinuxImmediateHostEntry
{
    public static Task Boundaries() => NifNativeTypeHostChecks.Run(NifNativeLinuxImmediateHostChecks.Boundaries);

    public static Task Ownership() => NifNativeTypeHostChecks.Run(NifNativeLinuxImmediateHostChecks.Ownership);

    public static Task Containment() => NifNativeTypeHostChecks.Run(NifNativeLinuxImmediateHostChecks.Containment);
}
