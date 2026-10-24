namespace Erlang.Tests;

internal static class EtsInspection420Names
{
    internal const string BindingTest = "runtime/ets-inspection420/authentic-native4-rollback", PrivilegeTest = "runtime/ets-inspection420/live-process-superuser-domain", RawTest = "runtime/ets-inspection420/actual-fixed-dead-key-inspection";
    internal const string Internal = "erts_internal", Ets = "ets", Super = "ets_super_user", First = "ets_raw_first", Next = "ets_raw_next", Binary = "ets_lookup_binary_info", New = "new", Name = "ets_inspection420", Named = "named_table", Private = "private", Insert = "insert", Lookup = "lookup", Delete = "delete", Fix = "safe_fixtable", True = "true", False = "false", Bad = "bad", Ok = "ok", End = "$end_of_table", Badarg = "badarg";
    internal const string Failure = "Actual ETS inspection420 HOST control failed";
    internal static readonly (string Function, int Arity)[] Signatures = [(Super, 1), (First, 1), (Next, 2), (Binary, 2)];
}
