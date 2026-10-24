using System.Reflection;

namespace Erlang.Tests;

internal static class PersistentArena416HostNames
{
    internal const string ValuesTest = "HOST/library416/persistent-authoritative-arena-all-term-values";
    internal const string RetiredTest = "HOST/library416/persistent-active-extents-exclude-real-read-leases";
    internal const string RollbackTest = "HOST/library416/persistent-arena-failed-stage-owner-and-cancel";
    internal const string WeakTest = "HOST/library416/persistent-native-payload-does-not-retain-source-graph";
    internal const string SlotsTest = "HOST/library416/persistent-native-source-wrap-clone-grow-shrink";
    internal const string Hash = "Hash";
    internal const int SlotSearchLimit = 1024;
    internal const int InitialCapacity = 8;
    internal const int WrapSlot = 7;
    internal const int AddedKeys = 7;
    internal const int AddedKeyBase = 10000;
    internal const int GrownCapacity = 32;
    internal const string WritersTest = "HOST/library416/persistent-native-table-two-actor-put-new";
    internal const string StoreType = "Erlang.PersistentArenaStore416";
    internal const string Put = "Put", Get = "Get", GetAll = "GetAll", Erase = "Erase";
    internal const string Info = "Info", Allocations = "Allocations", Snapshot = "AcquireSnapshot", Close = "CloseAsync";
    internal const string Reclaim = "WaitForReclamationAsync";
    internal const string Pairs = "Pairs", Count = "Count", Memory = "Memory", Capacity = "Capacity";
    internal const string TableBytes = "ActualTableRequestedBytes", PairBytes = "ActivePairRequestedBytes";
    internal const string AllocatedPairs = "AllocatedPairBlocks", AllocatedPairBytes = "AllocatedPairBytes";
    internal const string AllocatedTables = "AllocatedTableBlocks", AllocatedTableBytes = "AllocatedTableBytes";
    internal const string BinaryReferenced = "ReferencedByteSize", Stop = "Stop";
    internal const string Key = "arena_key416", OtherKey = "other_arena_key416", Shared = "shared416";
    internal const string RecordModule = "arena_record_module416", RecordName = "arena_record416", Field = "field416";
    internal const string ExternalNode = "arena416@external", Atom = "arena_atom416", Fallback = "fallback416";
    internal const string Failure = "Persistent authoritative native graph/table owner control failed";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const int Deep = 8192;
    internal static readonly byte[] ResourceBytes = [4, 1, 6, 0];
}
