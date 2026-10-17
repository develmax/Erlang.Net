namespace Erlang.NifNativeBackend;

public static class NifNativeMapSizeHostEntry
{
    public static Task ShapeAndOutput() => NifNativeBinaryHostChecks.Run(NifNativeMapSizeHostChecks.ShapeAndOutput);

    public static Task PureEnvironment() => NifNativeBinaryHostChecks.Run(NifNativeMapSizeHostChecks.PureEnvironment);

    public static Task OwnerLifetime() => NifNativeBinaryHostChecks.Run(NifNativeMapSizeHostChecks.OwnerLifetime);
}
