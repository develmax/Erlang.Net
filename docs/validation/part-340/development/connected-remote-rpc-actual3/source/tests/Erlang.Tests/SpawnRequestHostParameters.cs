namespace Erlang.Tests;

internal static class SpawnRequestHostParameters
{
    public const string Test = "runtime/spawn-request-abandon-owner-identity-death-order-and-resource-failure";
    public const string Failure = "Spawn request lifecycle contract failed";
    public const string OwnerDeath = "spawn_request_owner_death";
    public const string MonitorInfoOption = "info";
    public const int ResourceCapacity = 2;
    public const int RaceAttempts = 64;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
}
