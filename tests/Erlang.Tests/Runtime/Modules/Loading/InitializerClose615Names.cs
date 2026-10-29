using System.Reflection;

namespace Erlang.Tests;

internal static class InitializerClose615Names
{
    internal const string ClosedCase = "runtime/initializer615/terminal-mailbox-before-stop-cancel";
    internal const string LiveCase = "runtime/initializer615/live-unrelated-cancellation-remains-visible";
    internal const string Failure = "Initializer terminal mailbox cancellation was misclassified.";
    internal const string Sentinel = "initializer615-live-output-cancellation";
    internal const string Gate = "gate";
    internal const string Processes = "processes";
    internal const string Initializer = "initializer";
    internal const string Completion = "Completion";
    internal const string Stopped = "stopped";
    internal const string Terminated = "Terminated";
    internal const string Closed = "closed593";
    internal const string Done = "Done";
    internal const string StateObservation = "INITIALIZER615 token={0} completion={1} disposal={2}";
    internal const string Request = "io_request";
    internal const string PutCharacters = "put_chars";
    internal const string Unicode = "unicode";
    internal const string Kill = "kill";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
