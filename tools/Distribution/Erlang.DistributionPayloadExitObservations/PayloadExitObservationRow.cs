using DistributionPacketContrasts;

namespace DistributionPayloadExitObservations;

internal sealed record PayloadExitObservationRow(
    string Name,
    PacketProcessResult? Process,
    string? Stage,
    bool Captured,
    string? Error
);
