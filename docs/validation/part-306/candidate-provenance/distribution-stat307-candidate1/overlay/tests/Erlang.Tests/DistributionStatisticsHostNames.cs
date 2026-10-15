using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionStatisticsHostNames
{
    internal const string Test = "runtime/distribution-statistics-accounted-storage-generation";
    internal const string Failure = "Distribution statistics contract failed";
    internal const string Function = "dist_get_stat";
    internal const string Statistics = "GetStatistics";
    internal const string Local = "stat_local@localhost";
    internal const string Remote = "stat_remote@localhost";
    internal const string Other = "stat_other@localhost";
    internal const string Payload = "stat_payload";
    internal const string SetNode = "setnode";
    internal const string Kernel = "net_kernel";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
}
