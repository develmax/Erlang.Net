namespace DistributionPacketContrasts;

internal sealed record PacketProcessResult(
    int ExitCode,
    bool TimedOut,
    bool RawComplete
);
