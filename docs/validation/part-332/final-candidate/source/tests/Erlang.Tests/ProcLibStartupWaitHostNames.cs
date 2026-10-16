namespace Erlang.Tests;

internal static class ProcLibStartupWaitHostNames
{
    public const string Test = "host_proc_lib_queued_startup_reply_precedes_invalid_timeout";
    public const string InterpretedTest = "host_interpreted_queued_receive_precedes_invalid_timeout";
    public const string Type = "Erlang.Otp.ProcLibStartup";
    public const string Method = "WaitInitialization";
    public const string Ack = "ack";
    public const string Nack = "nack";
    public const string Down = "DOWN";
    public const string InvalidTimeout = "invalid_timeout";
    public const string Reply = "arbitrary_startup_return";
    public const string Unrelated = "unrelated_startup_mail";
    public const string Erlang = "erlang";
    public const string ProcessInfo = "process_info";
    public const string Monitors = "monitors";
    public const string Ok = "ok";
    public const string Error = "error";
    public const string Matched = "matched";
    public const string Failure = "Queued receive timeout precedence contract failed";
    public const string SimpleExpression = "begin self()!ready,receive ready->matched after invalid->timeout end end";
    public const string ExpressionOnce = "begin put(eval_count,0),self()!ready,R=receive ready->matched after begin put(eval_count,get(eval_count)+1),invalid end->timeout end,{R,get(eval_count)} end";
}
