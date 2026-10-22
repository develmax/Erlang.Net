namespace Erlang;

internal static class HamtNativeNames430
{
    internal const string Debug = "erts_debug", Internal = "erts_internal";
    internal const string Info = "map_info", Children = "map_hashmap_children", Type = "term_type";
    internal const int Arity = 1;
    internal const string Foreign = "HAMT native operations require the actual runtime and exact signature";
    internal const string Conflict = "HAMT native group conflicts with an existing source/provider";
    internal const string Unsupported = "Hosting extension has no original OTP term header";
    internal static readonly (string Module, string Function, int Arity)[] Signatures =
        [(Debug, Info, Arity), (Internal, Children, Arity), (Internal, Type, Arity)];
}
