namespace Erlang;

internal static class LibraryStartupObservationNames
{
    internal const string SetupBeforePrepare = "setup-before-prepare";
    internal const string PurgerPermitAcquired = "purge-permit-acquired";
    internal const string CleanupStarted = "cleanup-started";
    internal const string CleanupRetirementMarked = "cleanup-core-retirement-marked";
}
