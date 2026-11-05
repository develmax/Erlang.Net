namespace Erlang;

internal static class GenEventRequests
{
    internal const string Notify = "notify";
    internal const string SyncNotify = "sync_notify";
    internal const string Call = "call";
    internal const string AddHandler = "add_handler";
    internal const string AddSupHandler = "add_sup_handler";
    internal const string DeleteHandler = "delete_handler";
    internal const string SwapHandler = "swap_handler";
    internal const string SwapSupHandler = "swap_sup_handler";
    internal const string WhichHandlers = "which_handlers";
    internal const string GetModules = "get_modules";
    internal const string Stop = "stop";
    internal const string System = "system";
}
