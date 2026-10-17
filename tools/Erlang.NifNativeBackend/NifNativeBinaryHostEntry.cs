namespace Erlang.NifNativeBackend;

public static class NifNativeBinaryHostEntry
{
    public static Task Inspection() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.Inspection);

    public static Task Iolists() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.Iolists);

    public static Task AllocationAcrossCalls() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.AllocationAcrossCalls);

    public static Task ReadonlyReallocation() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.ReadonlyReallocation);

    public static Task DeferredComposites() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.DeferredComposites);

    public static Task SubviewLifetimes() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.SubviewLifetimes);

    public static Task ExceptionalCleanup() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.ExceptionalCleanup);

    public static Task ExceptionGraph() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.ExceptionGraph);

    public static Task DeepGraph() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.DeepGraph);

    public static Task Containment() => NifNativeBinaryHostChecks.Run(NifNativeBinaryHostChecks.Containment);
}
