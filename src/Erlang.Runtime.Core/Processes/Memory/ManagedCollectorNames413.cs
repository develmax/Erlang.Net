namespace Erlang;

internal static class ManagedCollectorNames413
{
    internal const string LiteralModule = "erts_literal_area_collector";
    internal const string TraceModule = "erts_trace_cleaner";
    internal const string Start = "start";
    internal const string Release = "release_area_switch";
    internal const string Copy = "send_copy_request";
    internal const string Check = "check";
    internal const string Clean = "send_trace_clean_signal";
    internal const string Init = "init";
    internal const string CheckGc = "check_gc";
    internal const string NeedGc = "need_gc";
    internal const string CopyLiterals = "copy_literals";
    internal const string NotSupported = "notsup";
    internal const string Closed = "Managed collector owner is closed";
    internal const string Foreign = "Managed collector requires its runtime's real actor";
    internal const string Invariant = "Managed independent-literal/legacy-session0 collector profile is not selected";
    internal const string Gate = "Managed collector callback requires runtime signal admission";
    internal const string Startup = "Original managed collector role could not be started";
    internal const string LiteralImage = "7AB3844E3AF2CCF3DF49626D0174C8EC314E3BE1E1566A916C0F62E5DCF43C75";
    internal const string TraceImage = "087ACD0A49AEA078EFB4D9997DBFDEDC207F400736AAD6FF09DD3CF5C706C9DC";
    internal static readonly (string Module, string Function, int Arity)[] Signatures =
    [(LiteralModule, Release, 0), (LiteralModule, Copy, 3), (TraceModule, Check, 0), (TraceModule, Clean, 1)];
}
