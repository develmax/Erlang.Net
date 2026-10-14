namespace Erlang;

internal static class DistributionPassThroughDiagnostics
{
    internal const string Flags = "The negotiated distribution flags are incompatible with the pass-through message codec.";
    internal const string NativeRecords = "Native record distribution was not negotiated.";
    internal const string UnsupportedControl = "This distribution control operation is not supported by the pass-through message codec.";
    internal const string PacketLimit = "The encoded distribution packet exceeds the configured byte limit.";
    internal const string PacketFormat = "The distribution pass-through packet is malformed.";
}
