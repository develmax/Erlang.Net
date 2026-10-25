using DistributionPacketContrasts;

namespace DistributionSignalObservations;

internal sealed record SignalObservationRow(
    string Name,
    PacketProcessResult? Process,
    string? Stage,
    bool Captured,
    string? Error
);
