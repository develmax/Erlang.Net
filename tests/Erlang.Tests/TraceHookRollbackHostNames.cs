namespace Erlang.Tests;

internal static class TraceHookRollbackHostNames
{
    internal const string Test = "runtime/queued-actor-trace-hook-failure-rollback";
    internal const string Failure = "Queued actor trace-hook rollback contract failed.";
    internal const string InjectedFailure = "Injected HOST trace admission failure.";
    internal const string IncomingTermProperty = "IncomingTerm";
    internal const string WorkerModule = "trace_rollback_worker";
    internal const string WorkerFunction = "run";
}
