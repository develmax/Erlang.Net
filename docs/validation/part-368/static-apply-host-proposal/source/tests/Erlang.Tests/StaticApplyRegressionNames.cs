namespace Erlang.Tests;

internal static class StaticApplyRegressionNames
{
    internal const string Test = "compiler/static-apply-three-resolution-effects-and-source-frames";
    internal const string Failure = "Static apply lowering changed binding, evaluation, native validation or source continuation behavior.";
    internal const string Module = "static_apply368_execution";
    internal const string Entry = "run";
    internal const string Apply = "apply";
    internal const string Tail = "tail";
    internal const string NonTail = "non_tail";
    internal const string InvalidQueue = "bad";
    internal const string First = "a";
    internal const string Second = "b";
    internal const string ImproperTail = "argument_tail";
    internal const string Shadow = "shadow";
    internal const string OtherModule = "static_apply368_other";
    internal const string OtherResult = "other_import";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string QueueFileName = "queue.erl";
    internal const string ErrorInfo = "error_info";
    internal const string FormatterModule = "module";
    internal const string Formatter = "erl_erts_errors";
    internal const int GetError = 417;
    internal const int NonTailCall = 22;
    internal const int ApplyArity = 3;
}
