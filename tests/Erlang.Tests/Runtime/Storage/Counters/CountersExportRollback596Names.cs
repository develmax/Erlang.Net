using System.Reflection;

namespace Erlang.Tests;

internal static class CountersExportRollback596Names
{
    internal const string RollbackCase = "runtime/counters596/late-export-failure-releases-actual-host-custody";
    internal const string CommitCase = "runtime/counters596/successful-export-commits-only-returned-wrappers";
    internal const string Export = "Export";
    internal const string HostCarrier = "CountersHost596";
    internal const string HostRootsField = "hostRoots596";
    internal const string ArrayGateField = "gate";
    internal const string UnknownModule = "unknown_counters_export596";
    internal const string UnknownFunction = "unknown";
    internal const string Failure = "Host-export rollback or returned-wrapper custody failed.";
    internal const string CleanupFailure = "Counters596 assertion and independent cleanup failed.";
    internal const string UnexpectedInvoke = "The unadmitted closure body must not execute during copy.";
    internal const string Observation = "counters596 rollback: host roots at late closure={0}, after failure={1}";
    internal const int UniqueBytes = 16;
    internal const int ExportOccurrences = 2;
    internal const long UnobservedHosts = -1;
    internal const long FailedCopyOccurrences = 1;
    internal const long HostsAfterRollback = 0;
    internal const long HostsAfterFirstRelease = 1;
    internal const ulong FirstCell = 0;
    internal const long InitialCellValue = 0;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
