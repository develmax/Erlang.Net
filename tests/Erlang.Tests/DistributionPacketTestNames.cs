namespace Erlang.Tests;

internal static class DistributionPacketTestNames
{
    internal const string Test = "serialization/distribution-pass-through-send-registered-framing-flags-bounds";
    internal const string RecipientNode = "packet-recipient@host";
    internal const string SenderNode = "packet-sender@host";
    internal const string GoldenNode = "a@b";
    internal const string Payload = "packet_payload";
    internal const string Head = "head";
    internal const string Recipient = "packet_sink";
    internal const string EmptyCookie = "";
    internal const string Module = "packet_record_module";
    internal const string Record = "packet_record";
    internal const string Field = "packet_field";
    internal const string Failure = "Distribution packet contract assertion failed";
    internal const string Accepted = "Distribution packet unexpectedly accepted a rejected input";
}
