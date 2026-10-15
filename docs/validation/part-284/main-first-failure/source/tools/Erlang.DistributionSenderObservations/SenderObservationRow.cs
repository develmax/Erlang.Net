using DistributionPacketContrasts;

namespace DistributionSenderObservations;

internal sealed record SenderObservationRow(
    string Name,
    PacketProcessResult? Process,
    string? Stage,
    bool Captured,
    string? Error
);
