namespace Erlang.NifNativeBackend;

public static class NifNativeMapMaterialized505HostEntry
{
    public static Task FlatPhysicalLayoutAndEquality()
        => NifNativeBinaryHostChecks.Run(NifNativeMapMaterialized505HostChecks.FlatPhysicalLayoutAndEquality);

    public static Task NativeBufferReleaseAndSavedIndex()
        => NifNativeBinaryHostChecks.Run(NifNativeMapMaterialized505HostChecks.NativeBufferReleaseAndSavedIndex);

    public static Task DeepNativeMapEquality()
        => NifNativeBinaryHostChecks.Run(NifNativeMapMaterialized505HostChecks.DeepNativeMapEquality);

    public static Task CrossRuntimeAndClosedOwner()
        => NifNativeMapMaterialized505HostChecks.CrossRuntimeAndClosedOwner();
}
