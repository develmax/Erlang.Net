namespace Erlang.Tests;

internal static class NativePayloadProviderHostParameters
{
    internal const nuint Initial = 7;
    internal const nuint Grown = 19;
    internal const nuint Shrunk = 3;
    internal const nuint OtherOwner = 5;
    internal const byte Payload = 113;
    internal const int ReplyCount = 1;
    internal const int AggregateInstance = 0;
    internal const int WarmupCaptures = 256;
    internal const int MeasuredCaptures = 4096;
    internal const int WakeupRaces = 24;
    internal const int ReleaseRacePeriod = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
