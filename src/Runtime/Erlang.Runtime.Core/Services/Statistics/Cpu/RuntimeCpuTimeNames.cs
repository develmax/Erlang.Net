namespace Erlang;

internal static class RuntimeCpuTimeNames
{
    internal const string Unavailable = "Logical VM user CPU attribution is unavailable";
    internal const string Platform = "No authenticated OS thread identity provider for this target";
    internal const string ThreadMissing = "Current OS thread is absent from the diagnostics thread catalog";
    internal const string ThreadIdentity = "CPU scope exited on a different physical OS thread";
    internal const string ScopeOrder = "CPU scopes must exit in synchronous nesting order";
    internal const string CounterRegression = "OS thread user CPU counter moved backwards";
    internal const string Libc = "libc";
    internal const string GetTid = "gettid";
}
