namespace Erlang.Tests;

internal static class BeamLoaderLifecycleNames
{
    internal const string Down = "DOWN";
    internal const string Process = "process";
    internal const string Unwind = "CallbackExecutionUnwind";
    internal const string Apply = "apply";
    internal const string CodeModule = "code";
    internal const string CodePurge = "purge";
    internal const string Failure = "Loader native projection mismatch";
    internal const string Load = "load_module";
    internal const string Exported = "function_exported";
    internal const string Purge = "purge_module";
    internal const string Duplicated = "duplicated";
    internal const string NotPurged = "not_purged";
    internal const string Exception = "exception";
    internal const string Alpha = "alpha";
    internal const string Beta = "beta";
    internal const string Wrong = "wrong";
    internal const string NonBinary = "not_binary";
    internal const int ConstantValue = 42;
    internal const int WatchdogSeconds = 10;
}
