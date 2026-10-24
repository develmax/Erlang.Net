namespace Erlang.Tests;

internal static class EtsDeleteOutside412Names
{
    internal const string ReentryTest = "runtime/ets-delete412/guard-outside-gate-keeps-unrelated-insert";
    internal const string ReplaceTest = "runtime/ets-delete412/guard-replaced-row-identity-survives";
    internal const string Ets = "ets", Erlang = "erlang", New = "new", Insert = "insert", Lookup = "lookup";
    internal const string SelectDelete = "internal_select_delete", Predicate = "is_atom", Gate = "gate";
    internal const string Table = "ets_delete_outside412", A = "a", B = "b", C = "c";
    internal const string First = "$1", Second = "$2", True = "true";
    internal const string Failure = "ETS deletion guard gate or captured object identity changed";
}
