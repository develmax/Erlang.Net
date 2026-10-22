namespace Erlang.Tests;

internal static class DistSpawn430HostNames
{
    internal const string AsyncTest = "runtime/dist-spawn430-real-busy-async-output-survives-caller-death";
    internal const string Erlang = "erlang", Flag = "process_flag", AsyncFlag = "async_dist", True = "true", Filler = "spawn430_filler";
    internal const string ErrorTest = "runtime/dist-spawn430-genuine-notalive-and-badopt-mailbox-protocol";
    internal const string RoundTripTest = "runtime/dist-spawn430-actual-two-runtime-request-reply";
    internal const string FrontierTest = "runtime/dist-spawn430-pending-owner-frontier-and-registration-identity";
    internal const string Module = "erts_internal", Function = "dist_spawn_request", Request = "spawn_request", Opt = "spawn_opt";
    internal const string Remote = "spawn430_remote@localhost", Target = "spawn430_target", Run = "run", Payload = "spawn430_payload", Tag = "spawn430_tag", Reply = "reply", ReplyTag = "reply_tag", No = "no", Bad = "bad430", BadArgument = "badarg", BadOptions = "badopt", NoConnection = "noconnection", Error = "error", Ok = "ok", Default = "spawn_reply", Monitor = "monitor";
    internal const string Failure = "Distribution spawn native owner protocol failed";
}
