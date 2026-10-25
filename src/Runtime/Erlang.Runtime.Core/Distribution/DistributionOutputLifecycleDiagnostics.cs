namespace Erlang;

internal static class DistributionOutputLifecycleDiagnostics
{
    internal const string DuplicateOwner = "A producer already owns a distribution output reservation";
    internal const string ForeignReservation = "The distribution output reservation belongs to another generation";
    internal const string InvalidExitOwner = "The exit continuation is not owned by this runtime";
    internal const string Retired = "The captured distribution output generation has retired";
    internal const string WouldBlock = "Immediate host output would block on capacity or an earlier owned output operation; use the async API";
}
