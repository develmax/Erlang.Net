internal static class GenStatemRetentionNames
{
    internal const string Scenario = "sys_retention";
    internal const string CallbackModule = "gen_statem_retention_probe";
    internal const string Init = "init";
    internal const string CallbackMode = "callback_mode";
    internal const string HandleEvent = "handle_event";
    internal const string Terminate = "terminate";
    internal const string Start = "start";
    internal const string Stop = "stop";
    internal const string Call = "call";
    internal const string Install = "install";
    internal const string State = "idle";
    internal const string Mode = "handle_event_function";
    internal const string Ok = "ok";
    internal const string KeepState = "keep_state";
    internal const string Reply = "reply";
    internal const string GetState = "get_state";
    internal const string GetStatus = "get_status";
    internal const string Suspend = "suspend";
    internal const string Resume = "resume";
    internal const string GarbageCollect = "garbage_collect";
    internal const string MailboxSignal = "changed";
    internal const string MachineType = "Erlang.GenStatemMachine";
    internal const string ContinuationTypePart = "Continuation";
    internal const string GraphFailure = "gen_statem continuation graph cannot be admitted";
    internal const string Report = "sys-retention-report.json";
    internal const string Failure = "Historical gen_statem machines remain in the live async continuation graph";
    internal const string TerminationFailure = "gen_statem terminate callback must run exactly once";
    internal const int Rounds = 32;
    internal const int MaximumRetainedMachines = 1;
    internal const int MaximumContinuationObjects = 100000;
    internal static readonly TimeSpan ObservationPollInterval = TimeSpan.FromMilliseconds(1);
}
