namespace Erlang;

internal static class GenEventApi
{
    internal const string Start = "start";
    internal const string StartLink = "start_link";
    internal const string StartMonitor = "start_monitor";
    internal const string Stop = "stop";
    internal const string Notify = "notify";
    internal const string SyncNotify = "sync_notify";
    internal const string AddHandler = "add_handler";
    internal const string AddSupHandler = "add_sup_handler";
    internal const string DeleteHandler = "delete_handler";
    internal const string SwapHandler = "swap_handler";
    internal const string SwapSupHandler = "swap_sup_handler";
    internal const string WhichHandlers = "which_handlers";
    internal const string Call = "call";
    internal const string SendRequest = "send_request";
    internal const string WaitResponse = "wait_response";
    internal const string ReceiveResponse = "receive_response";
    internal const string CheckResponse = "check_response";
    internal const string ReqidsNew = "reqids_new";
    internal const string ReqidsSize = "reqids_size";
    internal const string ReqidsAdd = "reqids_add";
    internal const string ReqidsToList = "reqids_to_list";
    internal const string WakeHib = "wake_hib";
    internal const string InitIt = "init_it";
    internal const string SystemContinue = "system_continue";
    internal const string SystemTerminate = "system_terminate";
    internal const string SystemCodeChange = "system_code_change";
    internal const string SystemGetState = "system_get_state";
    internal const string SystemReplaceState = "system_replace_state";
    internal const string FormatStatus = "format_status";
    internal const string FormatLog = "format_log";
    internal const string BehaviourInfo = "behaviour_info";
}
