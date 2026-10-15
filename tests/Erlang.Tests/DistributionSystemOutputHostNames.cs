namespace Erlang.Tests;

internal static class DistributionSystemOutputHostNames
{
    internal const string Test = "host_distribution_same_controller_owned_replies_under_output_pressure";
    internal const string Local = "owned_system@local";
    internal const string Remote = "owned_system@remote";
    internal const string Accepted = "ok";
    internal const string Filler = "owned_system_filler";
    internal const string Failure = "Owned distribution system output assertion failed";
    internal const int Creation = 3;
    internal const int RemoteId = 7;
    internal const int UnlinkId = 11;
    internal const int InitialAcceptedRequestCount = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
