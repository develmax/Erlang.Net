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
        KernelBootOriginal453Names.Test => TimeSpan.FromSeconds(KernelBootOriginal453Names.BudgetSeconds),
        ReUnicodeCancellation453Names.Test => TimeSpan.FromSeconds(ReUnicodeCancellation453Names.BudgetSeconds),
        NumericContract487Names.Test => TimeSpan.FromSeconds(NumericContract487Names.BudgetSeconds),
        _ => TimeSpan.FromSeconds(TestExecutionBudgetNames.DefaultSeconds)
    };
}
