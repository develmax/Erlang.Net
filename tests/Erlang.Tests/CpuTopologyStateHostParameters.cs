namespace Erlang.Tests;

internal static class CpuTopologyStateHostParameters
{
    public const int Exchanges = 8;
    public static TimeSpan Deadline { get; } = TimeSpan.FromSeconds(10);
}
