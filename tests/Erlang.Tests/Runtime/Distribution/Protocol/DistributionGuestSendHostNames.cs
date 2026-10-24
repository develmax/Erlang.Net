namespace Erlang.Tests;

internal static class DistributionGuestSendHostNames
{
    internal const string Test = "host_guest_connected_pid_send_routes_options_and_trace";
    internal const string Send = "send";
    internal const string Bang = "!";
    internal const string GroupLeader = "group_leader";
    internal const string NoSuspendSend = "send_nosuspend";
    internal const string NoSuspend = "nosuspend";
    internal const string NoConnect = "noconnect";
    internal const string Priority = "priority";
    internal const string Ok = "ok";
    internal const string InvalidOption = "guest_send_invalid_option";
    internal const string BadArgument = "badarg";
    internal const string Kill = "kill";
    internal const string First = "guest_send_first";
    internal const string Second = "guest_send_second";
    internal const string Third = "guest_send_third";
    internal const string GeneratedModule = "guest_send_generated";
    internal const string Run = "run";
    internal const string Source = "-module(guest_send_generated). -export([run/2]). run(P,M) -> P ! M.";
    internal const string Failure = "Connected guest PID send assertion failed";
    internal const int RemoteCreation = 3;
    internal const int Label = 42;
    internal const int InitialSerial = 8;
    internal const int LastCount = 4;
    internal const int HaltStatus = 19;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
