namespace Erlang.Tests;

internal static class DistributionGuestExitHostNames
{
    internal const string PublicTwoTest = "host_guest_connected_exit2_public_control";
    internal const string PublicThreeTest = "host_guest_connected_exit3_public_control";
    internal const string LifecycleTest = "host_guest_connected_exit_profiles_tokens_and_ownership";
    internal const string Failure = "Connected public guest exit assertion failed";
    internal const string Reason = "guest_explicit_exit_reason";
    internal const string ExitSignal = "exit_signal";
    internal const string Priority = "priority";
    internal const string InvalidOption = "invalid_guest_exit_option";
    internal const string ProcessFlag = "process_flag";
    internal const string AsyncDistribution = "async_dist";
    internal const string GroupLeader = "group_leader";
    internal const string SendFunction = "send";
    internal const string SequentialTraceMember = "SequentialTrace";
    internal const string TokenMember = "Token";
    internal const string TokenToTerm = "ToTerm";
    internal const string GateMember = "gate";
    internal const string DrainMember = "DrainOwnedOutputOperations";
    internal const string GeneratedModule = "guest_exit_generated";
    internal const string GeneratedFunction = "run";
    internal const string GeneratedSource = "-module(guest_exit_generated). -export([run/2]). run(P,R) -> erlang:exit(P,R).";
    internal const string ClosureModule = "guest_exit_closure";
    internal const string ClosureFunction = "make";
    internal const string ClosureSource = "-module(guest_exit_closure). -export([make/0]). make() -> fun(X) -> X end.";
    internal const int InitialSerial = 8;
    internal const int LastCount = 4;
    internal const int Label = 42;
    internal const int MaximumStagedPackets = 1024;
    internal const int HaltStatus = 23;
    internal const int SmallPacketLimit = 8;
    internal const int UnsupportedAliasFlag = 4;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
