namespace Erlang.Tests;

internal static class QueueCallbackRegressionNames
{
    internal const string Success = "ok";
    internal const string ListsModule = "lists";
    internal const string ListReverse = "reverse";
    internal const string Test = "queue/higher-compiled-apply-badarity-and-rear-expansion-frames";
    internal const string Failure = "Compiled queue callback error reason or full source stack differs from the pinned regression contract.";
    internal const string Expansion = "expansion";
    internal const string BadArityCapture = "badarity";
    internal const string Invoke = "invoke";
    internal const string Wrong = "wrong";
    internal const string Entry = "run";
    internal const string FilterFront = "filter_f";
    internal const string FilterRear = "filter_r";
    internal const string ListFilterMap = "filtermap_1";
    internal const string DeleteFront = "delete_with_front";
    internal const string CallbackTail = "callback_tail";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string ErrorInfo = "error_info";
    internal const string FormatterModule = "module";
    internal const string StdlibFormatter = "erl_stdlib_errors";
    internal const int QueueCallbackArity = 2;
    internal const int CaptureArity = 1;
    internal const int NoArgumentsArity = 0;
    internal const int Outcomes = 7;
}
