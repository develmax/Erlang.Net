using System.Reflection;

namespace Erlang.Tests;

internal static class StandardBootCollectors415Names
{
    internal const string ChildSwitch = "--standard79-collectors415-child";
    internal const string Receipt = "collector-lifecycle.json";
    internal const string Failure = "Automatic original collector actors did not retain/release their actual source lifecycle";
    internal const string Gate = "gate";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const string ExecutionIdentity = "ExecutionIdentity";
    internal const string Image = "Image";
    internal const string Terminated = "Terminated";
    internal const string IsSystem = "IsSystemProcess";
    internal const string Done = "Done";
    internal const string Reservation = "Reservation";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
