namespace Erlang.Tests;

internal static class DistributionExitLifecycleHostNames
{
    internal const string Test = "host_distribution_owned_exit_under_output_pressure";
    internal const string Local = "owned_exit@local";
    internal const string Remote = "owned_exit@remote";
    internal const string Filler = "owned_exit_filler";
    internal const string Failure = "Owned distribution exit lifecycle assertion failed";
    internal const int Creation = 3;
    internal const int RemoteId = 7;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
