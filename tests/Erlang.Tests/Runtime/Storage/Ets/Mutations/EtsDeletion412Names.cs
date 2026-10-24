namespace Erlang.Tests;

internal static class EtsDeletion412Names
{
    internal const string KernelTest = "runtime/ets-deletion412/real-four-kind-storage-and-counts";
    internal const string InvalidTest = "runtime/ets-deletion412/invalid-specification-keeps-live-rows";
    internal const string AccessTest = "runtime/ets-deletion412/nonowner-write-refusal-keeps-both-table-kinds";
    internal const string RegistrationTest = "runtime/ets-deletion412/foreign-registry-and-atomic-conflict-refusal";
    internal const string Protected = "protected";
    internal const string Ets = "ets", New = "new", Insert = "insert", Lookup = "lookup", Delete = "delete";
    internal const string InternalDelete = "internal_select_delete", DeleteAll = "internal_delete_all";
    internal const string Table = "deletion_host412", Named = "named_table", Public = "public", Private = "private";
    internal const string Set = "set", Bag = "bag", Duplicate = "duplicate_bag", Ordered = "ordered_set";
    internal const string Keep = "keep", Remove = "remove", Wildcard = "_", Missing = "missing_deletion412";
    internal const string Undefined = "undefined";
    internal const string Invalid = "invalid_specification412", Holder = "retained_holder412";
    internal const string Failure = "ETS deletion storage/control mismatch";
}
