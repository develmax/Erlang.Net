namespace Erlang;

internal static class EtsCounterNames417
{
    internal const string Module = "ets", Update = "update_counter";
    internal const string TableType = "table_type", BadKey = "badkey";
    internal const string Foreign = "ETS counter requires its exact live runtime and registry";
    internal const string Conflict = "ETS counter refuses existing or source-owned exports";
    internal const string Signature = "ETS counter signature is not admitted";
    internal static readonly (string Function, int Arity)[] Signatures = [(Update, 3), (Update, 4)];
}
