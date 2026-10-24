namespace Erlang.Tests;

internal static class DistributionGroupLeaderOutputHostNames
{
    internal const string Test = "host_registered_group_leader_connected_output_ownership";
    internal const string GroupLeader = "group_leader";
    internal const string Following = "group_leader_following_send";
    internal const string InvalidPid = "group_leader_invalid_pid";
    internal const string Kill = "kill";
    internal const string BadArgument = "badarg";
    internal const string ThirdNode = "group_leader_third@remote";
    internal const string Failure = "Connected group leader output assertion failed";
    internal const int ExternalLeaderId = 41;
    internal const int ExternalLeaderCreation = 7;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
