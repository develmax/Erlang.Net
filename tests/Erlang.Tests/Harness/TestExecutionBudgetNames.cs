namespace Erlang.Tests;

internal static class TestExecutionBudgetNames
{
    internal const int DefaultSeconds = 15;
    internal const int DeepPreparationAndCleanupSeconds = 15;
    // Separate cold preparation allowance, not an Erlang execution deadline.
    internal const int FullComparisonPreparationSeconds500 = 30;
}
