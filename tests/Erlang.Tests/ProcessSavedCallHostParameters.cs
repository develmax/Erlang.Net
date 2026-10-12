namespace Erlang.Tests;

internal static class ProcessSavedCallHostParameters
{
    public const int Capacity = 8;
    public const int RingCapacity = 2;
    public const int DeathRounds = 32;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
