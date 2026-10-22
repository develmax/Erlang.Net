namespace Erlang;

internal static class EtsInspectionNames420
{
    internal const string Module = "erts_internal", Formatter = "erl_erts_errors", Ok = "ok", SuperUser = "ets_super_user", First = "ets_raw_first", Next = "ets_raw_next", Binary = "ets_lookup_binary_info";
    internal const string Foreign = "ETS inspection requires its exact live runtime and registry", Conflict = "ETS inspection refuses conflicting or source-owned entries", Signature = "ETS inspection native signature is not admitted";
    internal static readonly (string Function, int Arity)[] Signatures = [(SuperUser, 1), (First, 1), (Next, 2), (Binary, 2)];
}
