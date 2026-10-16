namespace Erlang.Tests;

internal static class DistributionControlWaitHostNames
{
    internal const string QueryTest = "host_guest_distribution_wait_public_owner_query";
    internal const string KillTest = "host_guest_distribution_wait_inbound_kill_before_drain";
    internal const string ContractsTest = "host_guest_distribution_wait_messages_and_retained_unlink";
    internal const string Failure = "Guest distribution control wait assertion failed";
    internal const string GeneratedModule = "guest_control_wait323";
    internal const string Link = "block_link";
    internal const string Send = "block_send";
    internal const string GroupLeader = "block_group_leader";
    internal const string Exit = "block_exit";
    internal const string Receive = "block_send_receive";
    internal const string Unlink = "block_unlink";
    internal const string Marker = "control_wait_payload";
    internal const string First = "control_wait_first";
    internal const string Last = "control_wait_last";
    internal const string Reason = "control_wait_exit_reason";
    internal const string Proceed = "control_wait_proceed";
    internal const string Monitor = "monitor";
    internal const string SendFunction = "send";
    internal const string Source = """
        -module(guest_control_wait323).
        -export([block_link/1,block_send/2,block_group_leader/1,block_exit/2,block_send_receive/2,block_unlink/1]).
        block_link(P) -> process_flag(trap_exit,true), erlang:link(P).
        block_send(P,M) -> process_flag(trap_exit,true), erlang:send(P,M).
        block_group_leader(P) -> process_flag(trap_exit,true), erlang:group_leader(self(),P).
        block_exit(P,R) -> process_flag(trap_exit,true), erlang:exit(P,R).
        block_send_receive(P,M) -> process_flag(trap_exit,true), erlang:send(P,M),
            receive A -> receive B -> receive C -> {A,B,C} end end end.
        block_unlink(P) -> process_flag(trap_exit,true), erlang:link(P),
            receive control_wait_proceed -> erlang:unlink(P) end.
        """;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
