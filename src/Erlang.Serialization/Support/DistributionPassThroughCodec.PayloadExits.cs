namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    internal static bool IsPayloadExitPacket(ReadOnlySpan<byte> packet)
    {
        if (packet.IsEmpty || packet[0] != DistributionPassThroughLayout.Marker)
            return false;
        int? opcode = EtfControlOpcodeProbe.Read(packet[DistributionPassThroughLayout.MarkerBytes..]);

        return opcode is DistributionControlOpcodes.PayloadExit or DistributionControlOpcodes.PayloadExitTrace or
            DistributionControlOpcodes.PayloadExit2 or DistributionControlOpcodes.PayloadExit2Trace or DistributionControlOpcodes.PayloadMonitorExit;
    }

    internal static DistributionPayloadExitEnvelope DecodePayloadExitEnvelope(
        ReadOnlySpan<byte> packet,
        EtfDecodeOptions options,
        ulong negotiatedFlags
    )
    {
        ValidateFlags(negotiatedFlags);
        if (packet.Length > ExternalTermFormat.DefaultMaxBytes || packet.IsEmpty || packet[0] != DistributionPassThroughLayout.Marker)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        var control = ReadTerm(packet[DistributionPassThroughLayout.MarkerBytes..], options, ExternalTermFormat.DefaultMaxBytes);
        if (control.Term is not TupleTerm tuple || !IsPayloadExitControl(tuple))
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        ValidateFeatures(tuple, negotiatedFlags);
        int offset = checked(DistributionPassThroughLayout.MarkerBytes + control.Used);

        // Keep all remaining bytes opaque, including malformed or missing reason.
        return new(tuple, packet[offset..].ToArray());
    }

    public static byte[] EncodePayloadExit(
        TupleTerm control,
        Term reason,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload,
        EtfEncodingOptions? options = null
    )
    {
        options ??= new();
        ValidateFlags(negotiatedFlags);
        if (options is { Local: true } || options is { CompressionLevel: not EtfEncodingParameters.Uncompressed })
            throw new NotSupportedException(DistributionPayloadExitDiagnostics.WireOptions);
        if ((negotiatedFlags & DistributionPassThroughFlags.ExitPayload) == 0)
            throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
        if (!IsPayloadExitControl(control))
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        ValidateFeatures(control, negotiatedFlags);
        ValidateFeatures(reason, negotiatedFlags);
        byte[] first = ExternalTermFormat.Encode(control, options);
        byte[] second = ExternalTermFormat.Encode(reason, options);
        int length = checked(DistributionPassThroughLayout.MarkerBytes + first.Length + second.Length);
        if (length > ExternalTermFormat.DefaultMaxBytes)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketLimit, nameof(reason));
        byte[] packet = new byte[length];
        packet[0] = DistributionPassThroughLayout.Marker;
        first.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes);
        second.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes + first.Length);

        return packet;
    }

    private static bool IsPayloadExitControl(TupleTerm control)
    {
        if (control.Items.Count == 0 || control.Items[DistributionControlOpcodes.OpcodeIndex] is not Integer opcode)
            return false;
        if (opcode.Value == DistributionControlOpcodes.PayloadMonitorExit)
            return control.Items.Count == DistributionControlOpcodes.MonitorArity &&
                control.Items[DistributionControlOpcodes.FromIndex] is Pid or Atom &&
                control.Items[DistributionControlOpcodes.TargetIndex] is Pid &&
                control.Items[DistributionControlOpcodes.MonitorReferenceIndex] is ReferenceTerm;
        bool traced = opcode.Value == DistributionControlOpcodes.PayloadExitTrace || opcode.Value == DistributionControlOpcodes.PayloadExit2Trace;
        bool ordinary = opcode.Value == DistributionControlOpcodes.PayloadExit || opcode.Value == DistributionControlOpcodes.PayloadExit2;

        return (traced || ordinary) && control.Items.Count == (traced
            ? DistributionControlOpcodes.PayloadExitArity + 1 : DistributionControlOpcodes.PayloadExitArity) &&
            control.Items[DistributionControlOpcodes.FromIndex] is Pid && control.Items[DistributionControlOpcodes.TargetIndex] is Pid;
    }
}
