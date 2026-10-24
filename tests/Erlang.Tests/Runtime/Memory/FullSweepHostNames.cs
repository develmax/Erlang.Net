namespace Erlang.Tests;

internal static class FullSweepHostNames
{
    internal const string Flag = "fullsweep_after";
    internal const string Error = "fullsweep_after did not change actual forced collection behavior";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
