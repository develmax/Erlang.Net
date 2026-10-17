namespace Erlang.Tests;

internal static class RuntimeReductionHostNames
{
    internal const string Test = "runtime/statistics-real-reductions-retired-actors-independent-cursors";
    internal const string Bump = "bump_reductions";
    internal const string Failure = "Real cooperative reduction accounting failed";
    internal const int FirstWork = 37;
    internal const int SecondWork = 53;
    internal const int CappedBump = 2000;
    internal const int RequestedBump = 4000;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
