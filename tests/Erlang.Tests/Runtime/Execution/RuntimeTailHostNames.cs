namespace Erlang.Tests;

public static class RuntimeTailHostNames
{
    public const string Test = "runtime/qualified-callback-tail-releases-source-frame";
    public const string Module = "runtime_tail_host";
    public const string CallbackModule = "runtime_tail_callback";
    public const string Probe = "probe";
    public const string Run = "run";
    public const string NonTail = "non_tail";
    public const string Fail = "fail";
    public const string Outer = "outer";
    public const string Kept = "kept";
    public const string Failure = "callback_failure";
    public const string Broken = "Runtime tail call retained or corrupted the source frame";
}
