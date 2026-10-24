namespace Erlang.Tests;

internal static class SpawnPriorityHostNames
{
    internal const string Module = "priority_host_probe";
    internal const string Record = "record";
    internal const string Erlang = "erlang";
    internal const string SpawnOpt = "spawn_opt";
    internal const string Priority = "priority";
    internal static readonly string[] Levels = ["low", "normal", "high", "max"];
    internal const string Done = "done";
    internal const string Delimiter = ",";
    internal const int ExpectedChildren = 4;
    internal const string Sequence = "max,high,normal,low";
    internal const string Unexpected = "Unexpected priority admission sequence: ";
    internal const int TimeoutSeconds = 10;
}
