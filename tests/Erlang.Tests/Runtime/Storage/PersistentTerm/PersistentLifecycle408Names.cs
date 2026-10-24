using System.Reflection;

namespace Erlang.Tests;

internal static class PersistentLifecycle408Names
{
    internal const string Test = "HOST/persistent-term408/actual-runtime-lifecycle-and-held-reader";
    internal const string Gate = "gate";
    internal const string Processes = "processes";
    internal const string Done = "Done";
    internal const string Retirement = "retirement";
    internal const string Closed = "Closed";
    internal const string ActivePairs = "ActivePairs";
    internal const string Kill = "kill";
    internal const int Pairs = 256;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
}
