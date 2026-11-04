internal static class TimerCompatibilityLimits
{
    internal static readonly TimeSpan WorkerDeadline = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan ExitDeadline = TimeSpan.FromSeconds(10);
}
