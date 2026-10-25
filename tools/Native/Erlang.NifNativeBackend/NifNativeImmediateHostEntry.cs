namespace Erlang.NifNativeBackend;

public static class NifNativeImmediateHostEntry
{
    public static Task Getter() => NifNativeTypeHostChecks.Run(NifNativeImmediateHostChecks.Getter);

    public static Task Ownership() => NifNativeTypeHostChecks.Run(NifNativeImmediateHostChecks.Ownership);

    public static Task Containment() => NifNativeTypeHostChecks.Run(NifNativeImmediateHostChecks.Containment);
}
