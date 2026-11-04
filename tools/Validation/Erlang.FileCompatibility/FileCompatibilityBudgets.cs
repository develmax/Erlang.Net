internal static class FileCompatibilityBudgets
{
    internal static readonly TimeSpan ServerReady = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan ServerJoin = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan ProbeReady = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan ProbeJoin = TimeSpan.FromSeconds(5);
}
