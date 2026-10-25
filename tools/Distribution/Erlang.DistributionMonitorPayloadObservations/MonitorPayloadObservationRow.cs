using DistributionPacketContrasts;

namespace DistributionMonitorPayloadObservations;

internal sealed record MonitorPayloadObservationRow(
    string Name,
    PacketProcessResult? Process,
    string? Stage,
    bool Captured,
    string? Error
);
