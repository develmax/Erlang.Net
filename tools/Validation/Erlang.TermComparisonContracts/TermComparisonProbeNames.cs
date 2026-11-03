namespace Erlang.Tools;

internal static class TermComparisonProbeNames
{
    internal const string ManagedSwitch = "--runtime";
    internal const string BeamSwitch = "--beam-runtime";
    internal const string Usage = "Usage: Erlang.TermComparisonContracts (--runtime|--beam-runtime) SHARD";
    internal const string Completed = "Runtime comparison matrix shard passed";
    internal const int InvalidArgumentsExitCode = 2;
    internal const int FailureExitCode = 1;
}
