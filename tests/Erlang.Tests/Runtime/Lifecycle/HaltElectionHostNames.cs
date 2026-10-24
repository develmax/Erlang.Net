namespace Erlang.Tests;

internal static class HaltElectionHostNames
{
    internal const string Test = "runtime/halt/immediate-election-preserves-first-pending-result";
    internal const string GateField = "gate";
    internal const string Failure = "Pending halt election contract failed";
    internal const int InitialStatus = 37;
    internal const int TakeoverStatus = 71;
    internal const int LaterStatus = 93;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
