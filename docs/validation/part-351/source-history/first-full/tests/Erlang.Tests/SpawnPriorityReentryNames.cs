namespace Erlang.Tests;

internal static class SpawnPriorityReentryNames
{
    internal const string Module = "priority_reentry_host_probe";
    internal const string Bridge = "bridge";
    internal const string Target = "target";
    internal const string Done = "done";
    internal const string MissingAdmission = "Guest target executed outside its owner scheduler.";
    internal const int TimeoutSeconds = 10;
}
