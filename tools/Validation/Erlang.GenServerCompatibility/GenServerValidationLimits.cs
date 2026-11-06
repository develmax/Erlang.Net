internal static class GenServerValidationLimits
{
    internal const int RequiredArguments = 3;
    internal const int ArgumentsWithScenario = 4;
    internal const int SuccessExitCode = 0;
    internal const int FailureExitCode = 1;
    internal const int UsageExitCode = 2;
    internal static readonly TimeSpan OperationDeadline = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan BootDeadline = TimeSpan.FromSeconds(30);
}
