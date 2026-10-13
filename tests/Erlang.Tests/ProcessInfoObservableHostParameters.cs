namespace Erlang.Tests;

public static class ProcessInfoObservableHostParameters
{
    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(15);
    public const int DeathRounds = 16;
    public const int LogicalArity = 2;
}
