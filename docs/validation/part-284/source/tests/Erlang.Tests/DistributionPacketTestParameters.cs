namespace Erlang.Tests;

internal static class DistributionPacketTestParameters
{
    internal const ulong RecipientId = 72;
    internal const uint RecipientCreation = 1;
    internal const ulong SenderId = 18;
    internal const uint SenderCreation = 2;
    internal const int IgnoredRegisteredCookie = 471;
    internal const int GoldenPayload = 7;
    internal const ulong GoldenPidId = 1;
    internal const byte WrongMarker = 131;
    internal const byte CompressedEnvelope = 80;
    internal const byte LocalEnvelope = 121;
    internal const int ControlTagOffset = 2;
    internal const int UnknownOpcode = DistributionControlOpcodes.AlternativeActionSend + 1;

    internal static ReadOnlySpan<byte> GoldenSend =>
    [
        112, 131, 104, 3, 97, 2, 119, 0, 88, 119, 3, 97, 64, 98,
        0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 131, 97, 7
    ];
}
