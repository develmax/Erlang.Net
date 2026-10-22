namespace Erlang;

internal static class EtsAllNames420
{
    internal const string SenderName = "system";
    internal static readonly Atom Sender = Term.A(SenderName);
    internal const string Module = "ets", Function = "internal_request_all";
    internal const string Foreign = "ETS all request requires its own live runtime actor and registry";
    internal const string Conflict = "ETS all primitive conflicts with an existing binding";
}
