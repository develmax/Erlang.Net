namespace Erlang.Tests;

internal static class OwnedShutdown506Names
{
    internal const string Test = "runtime/owned-guest506/nested-foreign-invocation-refuses-enclosing-vm-shutdown";
    internal const string Module = "owned_shutdown506";
    internal const string Outer = "outer";
    internal const string Inner = "inner";
    internal const string Rejected = "rejected";
    internal const string Pending = "pending";
    internal const string Failure = "Nested owned calls admitted shutdown of an enclosing live runtime.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}
