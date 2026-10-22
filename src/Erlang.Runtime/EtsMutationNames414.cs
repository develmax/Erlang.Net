namespace Erlang;

internal static class EtsMutationNames414
{
    internal const string Module = "ets", InsertNew = "insert_new", Take = "take", DeleteObject = "delete_object", Update = "update_element";
    internal const string Default = "default", KeyPosition = "keypos", Position = "position";
    internal const string Foreign = "Bootstrap ETS mutations require the exact runtime and registry";
    internal const string Conflict = "Bootstrap ETS mutations refuse existing or source-owned bindings";
    internal const string Signature = "Bootstrap ETS mutation signature is not admitted";
    internal static readonly (string Function, int Arity)[] Signatures = [(InsertNew, 2), (Take, 2), (DeleteObject, 2), (Update, 3), (Update, 4)];
}
