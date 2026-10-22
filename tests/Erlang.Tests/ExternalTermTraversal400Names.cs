namespace Erlang.Tests;

internal static class ExternalTermTraversal400Names
{
    internal const string Test = "runtime/external-term-wide-shared-graph-bounded-work-and-domain-checks";
    internal const string Value = "wide_shared_value400";
    internal const string RemoteNode = "wide_shared400@remote";
    internal const string Failure = "External term traversal retained duplicate pending edges or lost domain validation";
    internal const int Width = 1048576;
    internal const long AuxiliaryAllocationBudgetBytes = 1048576;
}
