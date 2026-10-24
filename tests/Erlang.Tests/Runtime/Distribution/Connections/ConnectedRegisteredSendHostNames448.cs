namespace Erlang.Tests;

internal static class ConnectedRegisteredSendHostNames448
{
    internal const string Test = "runtime/distribution448/registered-target-ordinary-options-and-async";
    internal const string Receiver = "registered_receiver448";
    internal const string Module = "registered_send448";
    internal const string Bang = "bang";
    internal const string Async = "async_dist";
    internal const string ProcessFlag = "process_flag";
    internal const string Send = "send";
    internal const string NoSuspend = "send_nosuspend";
    internal const string NoConnect = "noconnect";
    internal const string Source = """
        -module(registered_send448).
        -export([bang/2]).
        bang(Target,Value) -> Target ! Value.
        """;
    internal const string Failure = "Connected registered-send delivery contract failed.";
}
