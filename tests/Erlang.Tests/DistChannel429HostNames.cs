namespace Erlang.Tests;

internal static class DistChannel429HostNames
{
    internal const string LegacyTest = "runtime/dist-channel429-preserves-actual-distribution-entry-connected-handle";
    internal const string Statistics = "dist_get_stat";
    internal const string SameTest = "runtime/dist-channel429-actual-self-controller-magic-input-output";
    internal const string CrossTest = "runtime/dist-channel429-real-cross-controller-pending-and-death";
    internal const string GenerationTest = "runtime/dist-channel429-real-generation-and-foreign-resource-refusal";
    internal const string Failure = "Native PID distribution channel protocol failed";
    internal const string Runtime = "erlang", Module = "erts_internal", Function = "create_dist_channel", Register = "register", SetNode = "setnode", Kernel = "net_kernel";
    internal const string Local = "channel429_local@localhost", Remote = "channel429_remote@localhost", Other = "channel429_other@localhost";
    internal const string Ok = "ok", Message = "message", BadArgument = "badarg", Undefined = "undefined", True = "true", False = "false", Kill = "kill";
    internal const string Flag = "process_flag", Scheduler = "scheduler", Get = "dist_ctrl_get_opt", Size = "get_size", Input = "dist_ctrl_input_handler", Put = "dist_ctrl_put_data", Data = "dist_ctrl_get_data", None = "none";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(12);
}
