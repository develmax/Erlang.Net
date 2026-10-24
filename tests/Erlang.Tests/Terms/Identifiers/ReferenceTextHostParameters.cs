namespace Erlang.Tests;

public static class ReferenceTextHostParameters
{
    public const int Allocations = (1 << 18) + 1;
    public const uint SecondWordReservedMask = (1u << 18) - 1;
    public const uint ExpectedCarry = 1u << 18;
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}
