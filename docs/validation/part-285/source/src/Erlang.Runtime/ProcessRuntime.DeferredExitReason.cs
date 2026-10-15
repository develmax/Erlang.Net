namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal DistributionDeferredExitReason CreateDeferredDistributionExitReason(
        ReadOnlyMemory<byte> payload,
        Func<bool>? additionalGateHeld = null
    )
        => new(
            payload,
            () => System.Threading.Monitor.IsEntered(gate) || additionalGateHeld?.Invoke() == true,
            DecodeDeferredDistributionExitReason
        );

    private EtfDecodedTerm DecodeDeferredDistributionExitReason(ReadOnlyMemory<byte> payload)
    {
        // AdmitExternalDecode captures the actual decode-time local epoch.
        // Its function factories and final graph validation remain outside gate.
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(DistributionDeferredExitReasonDiagnostics.Locked);
        if (payload.Length < EtfHeaderLayout.MinimumPacketBytes ||
            payload.Span[EtfHeaderLayout.TagOffset] is EtfTags.Compressed or EtfTags.Local)
            throw new ArgumentException(DistributionDeferredExitReasonDiagnostics.Payload, nameof(payload));

        return ImportExternalTerm(payload.Span);
    }
}
