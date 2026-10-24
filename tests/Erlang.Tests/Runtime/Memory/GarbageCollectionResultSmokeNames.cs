namespace Erlang.Tests;

internal static class GarbageCollectionResultSmokeNames
{
    internal const string Function = "gather_gc_info_result";
    internal const string Schedulers = "schedulers";
    internal const string Failure = "Registered GC result protocol contract failed.";
    internal const string Marker = "gc_result_smoke_marker";
    internal static readonly int[] Capacities = [1, 2, 4];
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
