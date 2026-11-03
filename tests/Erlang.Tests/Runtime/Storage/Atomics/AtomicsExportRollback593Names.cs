using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsExportRollback593Names
{
    internal const string RollbackCase = "runtime/atomics593/late-export-failure-releases-actual-host-custody";
    internal const string CommitCase = "runtime/atomics593/successful-export-commits-only-returned-wrappers";
    internal const string Export = "Export";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string HostRootsField = "hostRoots586";
    internal const string ArrayGateField = "gate";
    internal const string UnknownModule = "unknown_export593";
    internal const string UnknownFunction = "unknown";
    internal const string Failure = "Host-export rollback or returned-wrapper custody failed.";
    internal const string CleanupFailure = "Atomics593 assertion and independent cleanup failed.";
    internal const string UnexpectedInvoke = "The unadmitted closure body must not execute during copy.";
    internal const string Observation = "atomics593 rollback: host roots at late closure={0}, after failure={1}";
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
