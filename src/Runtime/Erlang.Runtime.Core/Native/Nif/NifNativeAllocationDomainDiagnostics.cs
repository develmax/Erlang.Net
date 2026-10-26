namespace Erlang;

internal static class NifNativeAllocationDomainDiagnostics
{
    internal const string UnknownMembership = "Native allocation domain membership is not active.";
    internal const string SnapshotMismatch = "Native allocation owner snapshot differs from its domain state.";
    internal const string NonemptyMembership = "Native allocation membership cannot end while actual payload blocks remain.";
    internal const string ExistingAllocationHistory = "Native allocation domain must be joined before payload allocation begins.";
    internal const string DifferentDomain = "Native allocation owner already belongs to another domain.";
    internal const string ClosedMembership = "Native allocation owner domain membership has ended.";
}
