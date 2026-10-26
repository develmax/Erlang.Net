namespace Erlang.Tests;

internal static class NewConnection501CapacityNames
{
    internal const string DefaultTest = "host/new-connection501-default-retained-capacity-and-cleanup";
    internal const string ExplicitTest = "host/new-connection501-explicit-quota-retained-identity-and-isolation";
    internal const string Module = "erts_internal";
    internal const string NewConnection = "new_connection";
    internal const string AbortPending = "abort_pending_connection";
    internal const string NodePrefix = "owned_capacity501_";
    internal const string NodeSuffix = "@localhost";
    internal const string Failure = "Native pending connection capacity501 invariant failed";
    internal const int RetainedNodes = 1025;
    internal const int ExplicitQuota = 2;
    internal const uint ConnectionIdMask = 0xffffff;
}
