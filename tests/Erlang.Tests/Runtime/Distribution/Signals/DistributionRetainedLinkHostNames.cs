namespace Erlang.Tests;

internal static class DistributionRetainedLinkHostNames
{
    internal const string Test = "host_guest_link_retained_ownership_and_async_distribution";
    internal const string Failure = "Retained connected guest LINK assertion failed";
    internal const string Function = "retained_link";
    internal const string Returned = "retained_link_returned";
    internal const string Source = """
        -module(guest_control_wait323).
        -export([retained_link/3]).
        retained_link(P,Flag,Owner) ->
            process_flag(async_dist,Flag),process_flag(trap_exit,true),erlang:link(P),
            Owner!{retained_link_returned,self()},receive retained_link_stop->ok end.
        """;
}
