namespace Erlang.Tests;

internal static class DistributionRegisteredInputHostNames
{
    internal const string Test = "host_distribution_registered_input_compiled";
    internal const string Module = "registered_distribution_input_host";
    internal const string Direct = "direct";
    internal const string Dynamic = "dynamic";
    internal const string Capture = "capture";
    internal const string Local = "registered_input@local";
    internal const string Remote = "registered_input@remote";
    internal const string Sink = "registered_input_sink";
    internal const string Payload = "registered_input_payload";
    internal const string Invalid = "registered_input_invalid";
    internal const string Accepted = "ok";
    internal const string Error = "error";
    internal const string Failure = "Registered distribution input assertion failed.";
    internal const int RemoteCreation = 3;
    internal const int PartialBits = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const string Source = """
        -module(registered_distribution_input_host).
        -export([direct/2,dynamic/4,capture/2]).
        direct(H,D) -> erlang:dist_ctrl_put_data(H,D).
        dynamic(M,F,H,D) -> apply(M,F,[H,D]).
        capture(H,D) ->
            try erlang:dist_ctrl_put_data(H,D)
            catch C:R:S -> {C,R,S}
            end.
        """;
}
