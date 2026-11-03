using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsAggregate594Names
{
    internal const string GetAllCase = "runtime/atomics594/persistent-get-all-later-area-rollback";
    internal const string SnapshotCase = "runtime/atomics594/persistent-arena-snapshot-later-area-rollback";
    internal const string PairsCase = "runtime/atomics594/persistent-pairs-later-area-rollback";
    internal const string KeyA = "aggregate594_a";
    internal const string KeyB = "aggregate594_b";
    internal const string ArenaField = "arena";
    internal const string AreasField = "areas";
    internal const string ImageField = "image";
    internal const string PairOwnerField = "owned";
    internal const string TableType = "Erlang.PersistentTable416";
    internal const string TableNamesType = "Erlang.PersistentTableNames416";
    internal const string RootIndex = "RootIndex";
    internal const string CapacityOffset = "CapacityOffset";
    internal const string SnapshotType = "Erlang.PersistentTermArenaSnapshot417";
    internal const string Materialize = "Materialize";
    internal const string PairsSelected = "PairsSelected594";
    internal const string TransactionType = "Erlang.AtomicsHostCopyTransaction591";
    internal const string TrackedLeasesField = "leases";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string HostRootsField = "hostRoots586";
    internal const string ArrayGateField = "gate";
    internal const string Failure = "A callback-free persistent aggregate left partial atomics output custody behind.";
    internal const string CleanupFailure = "Persistent aggregate rollback assertions or independent restoration/cleanup failed.";
    internal const string Observation = "atomics594 aggregate route={0}, host roots after later-area failure={1}";
    internal const int GetAllRoute = 0;
    internal const int SnapshotRoute = 1;
    internal const int PairsRoute = 2;
    internal const int FirstArea = 0;
    internal const int SecondArea = 1;
    internal const int MagicOffset = 0;
    internal const long CorruptionBit = 1;
    internal const long OneHost = 1;
    internal const long NoHosts = 0;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
