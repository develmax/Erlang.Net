namespace Erlang.Tests;

internal static class EtsRename418HostNames
{
    internal const string Basics = "HOST/library418/ets-rename-identity-collision-name-reuse-cleanup";
    internal const string Access = "HOST/library418/ets-rename-public-borrower-and-protected-private-refusal";
    internal const string RetiredName = "HOST/library418/ets-rename-reuses-real-pending-owner-retirement-name";
    internal const string Admission = "HOST/library418/ets-rename-own-native-binding-idempotence-conflicts";
    internal const string Undefined = "undefined";

    internal const string Failure = "Genuine ETS rename native/namespace ownership differs";
    internal const string Ets = "ets", New = "new", Whereis = "whereis", Delete = "delete", Rename = "rename", Insert = "insert", Lookup = "lookup";
    internal const string Gate = "gate", Kill = "kill";
    internal const string Old = "rename_host_old418", Next = "rename_host_next418", Named = "named_table";
    internal const string Public = "public", Protected = "protected", Private = "private";
    internal const int Key = 1, Initial = 42, Replacement = 99;
    internal static readonly string[] Protections = [Public, Protected, Private];
}
