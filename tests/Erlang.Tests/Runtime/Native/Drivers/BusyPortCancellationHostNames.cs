namespace Erlang.Tests;

internal static class BusyPortCancellationHostNames
{
    internal const string Test = "runtime/port-command-cancellation-before-prior-protocol-command-completes";
    internal const string Ports = "Ports";
    internal const string Send = "Send";
    internal const string AwaitCommands = "AwaitCommands";
    internal const byte CancelledMarker = 9;
}
