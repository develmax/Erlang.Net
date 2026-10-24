namespace Erlang.Tests;

internal static class ProcessSavedCallHostNames
{
    public const string Test = "runtime/saved-calls-chronological-ring-and-queued-death-cleanup";
    public const string Module = "saved_call_host";
    public const string Alpha = "alpha";
    public const string Beta = "beta";
    public const string Gamma = "gamma";
    public const string Failure = "Saved calls must track real exports chronologically and release queued flag requests on target death";
}
