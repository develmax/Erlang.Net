namespace Erlang.Tests;

internal static class EtsMutations414Names
{
    internal const string ValuesTest = "runtime/ets-mutations414/five-native-bodies-four-kinds-and-rollback";
    internal const string RaceTest = "runtime/ets-mutations414/atomic-insert-new-and-take-single-winners";
    internal const string UpdateRaceTest = "runtime/ets-mutations414/concurrent-update-fields-without-lost-writes";
    internal const string ReaderTest = "runtime/ets-mutations414/real-held-read-leases-survive-detach-and-retire";
    internal const string RightsTest = "runtime/ets-mutations414/nonowner-write-refusal-for-all-five-signatures";
    internal const string RegistrationTest = "runtime/ets-mutations414/foreign-conflict-rollback-and-default-provider-unchanged";
    internal const string Ets = "ets", New = "new", Insert = "insert", Lookup = "lookup", Delete = "delete";
    internal const string InsertNew = "insert_new", Take = "take", DeleteObject = "delete_object", Update = "update_element";
    internal const string Table = "mutation_host414", Named = "named_table", Public = "public", Protected = "protected";
    internal const string Ordered = "ordered_set", Set = "set", Bag = "bag", Duplicate = "duplicate_bag";
    internal const string Invalid = "invalid_update414";
    internal const string Failure = "Real ETS mutation414 storage/atomicity control failed";
    internal const int Actors = 8, Iterations = 80;
}
