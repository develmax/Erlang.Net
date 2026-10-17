namespace Erlang.NifNativeBackend;

public static class NifNativeComparisonHostEntry
{
    public static Task Ordering() => NifNativeBinaryHostChecks.Run(NifNativeComparisonHostChecks.Ordering);

    public static Task ExactEquality() => NifNativeBinaryHostChecks.Run(NifNativeComparisonHostChecks.ExactEquality);

    public static Task PendingReads() => NifNativeBinaryHostChecks.Run(NifNativeComparisonHostChecks.PendingReads);

    public static Task ReferenceUniqueness() => NifNativeBinaryHostChecks.Run(NifNativeComparisonHostChecks.ReferenceUniqueness);

    public static Task OwnershipBoundaries() => NifNativeBinaryHostChecks.Run(NifNativeComparisonHostChecks.OwnershipBoundaries);
}
