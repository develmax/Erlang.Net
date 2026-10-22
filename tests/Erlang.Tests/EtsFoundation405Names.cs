namespace Erlang.Tests;

internal static class EtsFoundation405Names
{
    internal const string GraphTest = "runtime/library405/owned-storage-detached-graph-and-rollback";
    internal const string LifetimeTest = "runtime/library405/owned-storage-table-identity-and-owner-death";
    internal const string ResourceTest = "runtime/library405/owned-storage-resource-retention-and-read-lease";
    internal const string CancellationTest = "runtime/library405/owned-storage-cancellation-close-and-halt";
    internal const string Failure = "Internal owned storage foundation control failed";
    internal const string Property = "EtsFoundation";
    internal const string Field = "etsFoundation";
    internal const string Create = "CreateOwnerTable";
    internal const string Store = "StoreTuple";
    internal const string Read = "ReadTuple";
    internal const string AcquireRead = "AcquireRead";
    internal const string Materialize = "Materialize";
    internal const string Remove = "RemoveTuple";
    internal const string Destroy = "DestroyOwnerTable";
    internal const string Snapshot = "Snapshot";
    internal const string ProtectionType = "Erlang.RuntimeEtsProtection";
    internal const string StoredObjectType = "Erlang.RuntimeEtsStoredObject";
    internal const string AccountingField = "accounting";
    internal const string Capture = "Capture";
    internal const string Close = "Close";
    internal const string Private = "Private";
    internal const string Protected = "Protected";
    internal const string Public = "Public";
    internal const string Ets = "ets";
    internal const string New = "new";
    internal const string Name = "owned_foundation405";
    internal const string Marker = "marker405";
    internal const string Foreign = "The storage actor belongs to a different runtime";
    internal const string UnsupportedFunction = "Owned function storage requires a reviewed function environment rebinding substrate";
    internal const int DeepConsCount = 20000;
    internal const int BigIntegerShift = 256;
    internal const int CollectionAttempts = 4;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}
