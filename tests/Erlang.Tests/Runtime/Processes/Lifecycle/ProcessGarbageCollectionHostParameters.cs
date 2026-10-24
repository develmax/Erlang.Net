namespace Erlang.Tests;

public static class ProcessGarbageCollectionHostParameters
{
    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(15);
    public const int DeathRounds = 16;
}
