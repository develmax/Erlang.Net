namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    public static byte[] EncodeExplicitPidExit(
        Pid sender,
        Pid target,
        Term reason,
        bool priority,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        EtfEncodingOptions? options = null,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes
    )
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        options ??= new();
        if (options.Local || options.CompressionLevel != EtfEncodingParameters.Uncompressed)
            throw new NotSupportedException(DistributionPayloadExitDiagnostics.WireOptions);
        bool alternative = priority && (negotiatedFlags & DistributionPassThroughFlags.AlternativeActionSignal) != 0;
        bool payload = alternative || (negotiatedFlags & DistributionPassThroughFlags.ExitPayload) != 0;
        TupleTerm control = alternative
            ? Term.Tuple(
                Term.I(DistributionControlOpcodes.AlternativeActionSend),
                Term.I(DistributionExplicitPidExitLayout.PriorityExitAction),
                sender,
                target
            )
            : payload
                ? Term.Tuple(Term.I(DistributionControlOpcodes.PayloadExit2), sender, target)
                : Term.Tuple(
                    Term.I(DistributionControlOpcodes.Exit2),
                    sender,
                    target,
                    reason
                );
        ValidateFeatures(control, negotiatedFlags);
        ValidateFeatures(reason, negotiatedFlags);
        long required = checked(DistributionPassThroughLayout.MarkerBytes + ExternalTermFormat.EncodedSize(control, options) +
            (payload ? ExternalTermFormat.EncodedSize(reason, options) : 0));
        if (required > maximumBytes)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        byte[] first = ExternalTermFormat.Encode(control, options);
        byte[] second = payload ? ExternalTermFormat.Encode(reason, options) : [];
        long actualBytes = checked((long)DistributionPassThroughLayout.MarkerBytes + first.Length + second.Length);
        if (actualBytes > maximumBytes)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        byte[] packet = new byte[(int)actualBytes];
        packet[0] = DistributionPassThroughLayout.Marker;
        first.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes);
        second.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes + first.Length);

        return packet;
    }

    // Explicit opt-in observation of this outgoing PID-only action; default ingress remains unchanged.
    public static DistributionDecodedProcessPacket DecodeExplicitPidExit(
        ReadOnlySpan<byte> packet,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        EtfDecodeOptions? options = null,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        if (packet.IsEmpty || packet.Length > maximumBytes || packet[0] != DistributionPassThroughLayout.Marker)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        options ??= new();
        var decoded = ReadTerm(packet[DistributionPassThroughLayout.MarkerBytes..], options, maximumBytes);
        if (decoded.Term is not TupleTerm control || control.Items.Count == 0 || control.Items[0] is not Integer opcode)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        int offset = checked(DistributionPassThroughLayout.MarkerBytes + decoded.Used);
        var items = control.Items;
        if (opcode.Value == DistributionControlOpcodes.AlternativeActionSend)
        {
            if ((negotiatedFlags & DistributionPassThroughFlags.AlternativeActionSignal) == 0)
                throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
            if (items.Count != DistributionExplicitPidExitLayout.Arity ||
                items[DistributionExplicitPidExitLayout.FlagsIndex] is not Integer flags ||
                flags.Value != DistributionExplicitPidExitLayout.PriorityExitAction ||
                items[DistributionExplicitPidExitLayout.SenderIndex] is not Pid ||
                items[DistributionExplicitPidExitLayout.TargetIndex] is not Pid)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        }
        else
        {
            bool legacy = opcode.Value == DistributionControlOpcodes.Exit2;
            bool payload = opcode.Value == DistributionControlOpcodes.PayloadExit2;
            if (!legacy && !payload || items.Count != (legacy ? DistributionControlOpcodes.ExitArity : DistributionControlOpcodes.PayloadExitArity) ||
                items[DistributionControlOpcodes.FromIndex] is not Pid || items[DistributionControlOpcodes.TargetIndex] is not Pid)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            if (payload && (negotiatedFlags & DistributionPassThroughFlags.ExitPayload) == 0)
                throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
            if (legacy)
            {
                if (offset != packet.Length)
                    throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
                ValidateFeatures(control, negotiatedFlags);

                return new(control, null);
            }
        }
        var reason = ReadTerm(packet[offset..], options, maximumBytes);
        if (offset + reason.Used != packet.Length)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        ValidateFeatures(control, negotiatedFlags);
        ValidateFeatures(reason.Term, negotiatedFlags);

        return new(control, reason.Term);
    }
}
