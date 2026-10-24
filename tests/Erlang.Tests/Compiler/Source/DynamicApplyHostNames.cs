namespace Erlang.Tests;

internal static class DynamicApplyHostNames
{
    internal const string Test = "compiler/dynamic-apply-tail-frames-and-deep-transfer";
    internal const string Fixture = "dynamic_apply370_host.erl";
    internal const string Module = "dynamic_apply370_host";
    internal const string Success = "ok";
    internal const string Entry = "run";
    internal const string Tail = "tail_apply";
    internal const string NonTail = "non_tail_apply";
    internal const string Failure = "Dynamic apply changed caller retention or deep source transfer.";
    internal const int CapturedCases = 6;
}
