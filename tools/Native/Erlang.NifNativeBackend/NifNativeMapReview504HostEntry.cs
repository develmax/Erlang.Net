namespace Erlang.NifNativeBackend;

public static class NifNativeMapReview504HostEntry
{
    public static Task SourceFunUniquePreconditions()
        => NifNativeBinaryHostChecks.Run(NifNativeMapReview504HostChecks.SourceFunUniquePreconditions);

    public static Task StaticLookupAllocations()
        => NifNativeBinaryHostChecks.Run(NifNativeMapReview504HostChecks.StaticLookupAllocations);

    public static Task IndexedStaticLookupMisses()
        => NifNativeBinaryHostChecks.Run(NifNativeMapReview504HostChecks.IndexedStaticLookupMisses);

    public static Task CollisionCmpUsesActualFirstRole()
        => NifNativeBinaryHostChecks.Run(NifNativeMapReview504HostChecks.CollisionCmpUsesActualFirstRole);
}
