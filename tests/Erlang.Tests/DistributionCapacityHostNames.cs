namespace Erlang.Tests;

internal static class DistributionCapacityHostNames
{
    internal const string Test = "runtime/distribution-capacity-wake-retire-and-epoch";
    internal const string Channel = "capacity-owned-channel";
    internal const string WaitMethod = "WaitForOutputCapacity";
    internal const string QueueGate = "gate";
    internal const string Failure = "Distribution capacity notification contract failed";
    internal const int ByteLimit = 8;
    internal const int BufferLimit = 2;
    internal const int SegmentLimit = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
