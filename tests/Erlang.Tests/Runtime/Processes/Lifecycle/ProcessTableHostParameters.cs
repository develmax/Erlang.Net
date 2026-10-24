namespace Erlang.Tests;

public static class ProcessTableHostParameters
{
    public const int SmallCapacity = 3;
    public const int BatchCapacity = 1024;
    public const int HeldChildren = 1001;
    public const int DefaultCapacity = 1048576;
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}
