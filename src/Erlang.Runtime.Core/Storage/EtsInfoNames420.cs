namespace Erlang;

internal static class EtsInfoNames420
{
    internal const string Module = "ets", Info = "info", Internal = "erts_internal", BinaryHelper = "ets_info_binary", Binary = "binary";
    internal const string Protection = "protection", KeyPosition = "keypos", Type = "type", Named = "named_table", Node = "node", Size = "size", Name = "name", Heir = "heir", Owner = "owner", Memory = "memory", Compressed = "compressed", WriteConcurrency = "write_concurrency", ReadConcurrency = "read_concurrency", Decentralized = "decentralized_counters", Id = "id";
    internal const string Fixed = "fixed", SafeFixed = "safe_fixed", MonotonicFixed = "safe_fixed_monotonic_time", Stats = "stats", Data = "data", Undefined = "undefined", None = "none";
    internal const string TableLabel = "Table: ", NameLabel = "Name: ", ObjectsLabel = "Objects: ", WordsLabel = "Words: ", TypeLabel = "Type: ", ProtectionLabel = "Protection: ", CompressedLabel = "Compressed: ", WriteLabel = "Write Concurrency: ", ReadLabel = "Read Concurrency: ", BucketSeparator = ": [", CloseBucket = "]", Comma = ",", Deleted = "*", NewLine = "\n";
    internal const string Foreign = "ETS information requires its exact live runtime and registry", Conflict = "ETS information refuses conflicting or source-owned entries", Signature = "ETS information native signature is not admitted";
    internal static readonly string[] Fields = [Protection, KeyPosition, Type, Named, Node, Size, Name, Heir, Owner, Memory, Compressed, WriteConcurrency, ReadConcurrency, Decentralized, Id];
    internal static readonly (string Function, int Arity)[] Signatures = [(Info, 1), (Info, 2)];
}
