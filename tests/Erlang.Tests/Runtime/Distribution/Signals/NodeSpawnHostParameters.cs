namespace Erlang.Tests;

internal static class NodeSpawnHostParameters
{
    public const int Capacity = 8;
    public const int Attempts = 50;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
