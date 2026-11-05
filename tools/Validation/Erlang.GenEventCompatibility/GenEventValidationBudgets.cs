internal static class GenEventValidationBudgets
{
    internal static readonly TimeSpan Initialization = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan StartupWorkerExit = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan Observation = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan ObservationWorkerExit = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan RuntimeDisposal = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan OriginalBoot = TimeSpan.FromSeconds(30);
}
