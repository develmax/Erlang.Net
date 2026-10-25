namespace Erlang;

internal static class DistributionConnectedParameters
{
    internal const long MaximumQueuedPacketBytes = 16 * 1024 * 1024;
    internal const int MaximumQueuedPackets = 1024;
    internal const int MaximumQueuedSegments = 16 * MaximumQueuedPackets;
}
