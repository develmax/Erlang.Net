namespace Erlang;

internal static class FileErrorMessages
{
    internal const string UndefinedScript = "no value returned from script";
    internal const string EvaluationClass = "~w: evaluation failed with reason ~w:~w and stacktrace ~w";
    internal const string EvaluationReason = "~w: evaluation failed with reason ~w and stacktrace ~w";
    internal const string Located = "~w: ~ts";
    internal const string BadArgument = "bad argument";
    internal const string SystemLimit = "a system limit was hit, probably not enough ports";
    internal const string Terminated = "the file server process is terminated";
}
