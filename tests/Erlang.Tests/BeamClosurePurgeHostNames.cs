using System.Reflection;

namespace Erlang.Tests;

internal static class BeamClosurePurgeHostNames
{
    internal const string Test = "runtime/beam-fun-entry-pending-purge-active-continuation";
    internal const string Broken = "BEAM fun entry bypassed pending purge or leaked an image lease";
    internal const string Prepare = "PreparePurge";
    internal const string Finish = "FinishPurge";
    internal const string ProcessInfo = "process_info";
    internal const string Status = "status";
    internal const string Kill = "kill";
    internal const int ActiveLease = 1;
    internal const int ContinuationLeases = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const BindingFlags Properties = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
}
