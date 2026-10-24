namespace Erlang.Tests;

internal static class EtsPayloadDrop406Names
{
    internal const string LeaseTest = "HOST/ets-owned-array406/last-read-lease-roots";
    internal const string CopyTest = "HOST/ets-owned-array406/copy-out-root-survives-payload";
    internal const string Acquire = "AcquireRead";
    internal const string Destroy = "DestroyOwnerTable";
    internal const string Materialize = "Materialize";
    internal const int CollectionAttempts = 12;
}
