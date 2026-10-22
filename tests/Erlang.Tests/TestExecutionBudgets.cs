namespace Erlang.Tests;

internal static class TestExecutionBudgets
{
    internal static TimeSpan For(string name) => name switch
    {
        HaltSubprocessHostNames.Test => TimeSpan.FromSeconds(HaltSubprocessHostNames.OuterHostBudgetSeconds),
        BeamLifecycleHostNames.DeepTest => BeamLifecycleHostNames.Deadline
            + TimeSpan.FromSeconds(TestExecutionBudgetNames.DeepPreparationAndCleanupSeconds),
        DebugDirtyOriginal424Names.Test => TimeSpan.FromSeconds(DebugDirtyOriginal424Names.OuterBudgetSeconds),
        HamtOriginal430Names.Test => TimeSpan.FromSeconds(HamtOriginal430Names.BudgetSeconds),
        _ => TimeSpan.FromSeconds(TestExecutionBudgetNames.DefaultSeconds)
    };
}
