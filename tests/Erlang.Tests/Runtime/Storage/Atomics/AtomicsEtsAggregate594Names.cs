using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsEtsAggregate594Names
{
    internal const string Case = "runtime/atomics594/ets-backed-snapshot-later-row-rollback";
    internal const string KeyA = "ets_aggregate594_a";
    internal const string KeyB = "ets_aggregate594_b";
    internal const string SnapshotType = "Erlang.PersistentTermReadSnapshot";
    internal const string OwnerField = "owner";
    internal const string PayloadField = "payload";
    internal const string NodesField = "nodes";
    internal const string KindProperty = "Kind";
    internal const string Materialize = "Materialize";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string HostRootsField = "hostRoots586";
    internal const string ArrayGateField = "gate";
    internal const string Failure = "A callback-free ETS snapshot retained partial host outputs after a later row failed.";
    internal const string CleanupFailure = "ETS snapshot assertions or independent payload restoration/cleanup failed.";
    internal const string Observation = "atomics594 ETS aggregate: host roots after malformed later row={0}";
    internal const int RootNode = 0;
    internal const int InvalidKind = int.MaxValue;
    internal const int RowCount = 2;
    internal const long OneHost = 1;
    internal const long NoHosts = 0;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
