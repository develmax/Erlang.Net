namespace Erlang.Tests;

internal static class HostDistributionTestNames
{
    internal const string Test = "runtime/distribution-host-controller-generation-queue-notification-lifetime";
    internal const string Channel = "owned-host-test-channel";
    internal const string OtherChannel = "other-host-test-channel";
    internal const string Data = "dist_data";
    internal const string Failure = "HOST distribution focus assertion failed";
    internal const string Killed = "kill";
    internal const string ForeignNode = "other-host-runtime@localhost";
    internal const int PayloadLimit = 8;
    internal const int BufferLimit = 2;
    internal const int RaceRepetitions = 64;
    internal const int SegmentLimit = 16;
    internal const int OverSegmentLimit = 17;
    internal const int HalfSegmentLimit = 8;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
