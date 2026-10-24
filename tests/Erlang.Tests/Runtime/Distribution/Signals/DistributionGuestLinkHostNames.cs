using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionGuestLinkHostNames
{
    internal const string LinkTest = "host_guest_connected_link_public_control";
    internal const string UnlinkTest = "host_guest_connected_unlink_inbound_public_control";
    internal const string LifecycleTest = "host_guest_connected_link_unlink_phases_ack_and_ownership";
    internal const string Failure = "Connected public guest link assertion failed";
    internal const string Marker = "guest_link_order_marker";
    internal const string ExitReason = "guest_link_exit_reason";
    internal const string InvalidOption = "guest_link_invalid_option";
    internal const string RuntimeGateMember = "gate";
    internal const string LinksMember = "Links";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string PriorityMember = "PriorityLinks";
    internal const string Priority = "priority";
    internal const string Disconnected = "noconnection";
    internal const string Send = "send";
    internal const string GroupLeader = "group_leader";
    internal const int UnlinkIdIndex = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
