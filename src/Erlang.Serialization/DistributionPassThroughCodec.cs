namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    public static byte[] Encode(
        DistributionPassThroughMessage message,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        EtfEncodingOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        ValidateControl(
            message.Control,
            requireSenderNegotiation: true,
            negotiatedFlags: negotiatedFlags,
            allowAlternativePidSend: true
        );
        ArgumentNullException.ThrowIfNull(message.Payload);
        options ??= new EtfEncodingOptions();
        long required = checked(DistributionPassThroughLayout.MarkerBytes +
            ExternalTermFormat.EncodedSize(message.Control, options) +
            ExternalTermFormat.EncodedSize(message.Payload, options));
        if (required > maximumBytes)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketLimit, nameof(message));
        ValidateFeatures(message.Control, negotiatedFlags);
        ValidateFeatures(message.Payload, negotiatedFlags);
        byte[] control = ExternalTermFormat.Encode(message.Control, options);
        byte[] payload = ExternalTermFormat.Encode(message.Payload, options);
        int actualBytes = checked(DistributionPassThroughLayout.MarkerBytes + control.Length + payload.Length);
        if (actualBytes > maximumBytes)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketLimit, nameof(message));
        byte[] packet = new byte[actualBytes];
        packet[0] = DistributionPassThroughLayout.Marker;
        control.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes);
        payload.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes + control.Length);

        return packet;
    }

    public static DistributionPassThroughMessage Decode(
        ReadOnlySpan<byte> packet,
        EtfDecodeOptions? options = null,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        Action<TupleTerm>? unsupportedControlValidator = null,
        bool allowAlternativePidSend = false
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        if (packet.Length > maximumBytes || packet.IsEmpty || packet[0] != DistributionPassThroughLayout.Marker)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        options ??= new EtfDecodeOptions();
        var control = ReadTerm(packet[DistributionPassThroughLayout.MarkerBytes..], options, maximumBytes);
        if (control.Term is not TupleTerm tuple)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        ValidateControl(
            tuple,
            unsupportedControlValidator,
            negotiatedFlags,
            allowAlternativePidSend: allowAlternativePidSend
        );
        int payloadOffset = checked(DistributionPassThroughLayout.MarkerBytes + control.Used);
        var payload = ReadTerm(packet[payloadOffset..], options, maximumBytes);
        if (payloadOffset + payload.Used != packet.Length)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));

        ValidateFeatures(tuple, negotiatedFlags);
        ValidateFeatures(payload.Term, negotiatedFlags);

        return new(tuple, payload.Term);
    }

    public static DistributionDecodedProcessPacket DecodeProcessPacket(
        ReadOnlySpan<byte> packet,
        EtfDecodeOptions? options = null,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        Action<TupleTerm>? unsupportedControlValidator = null,
        bool allowAlternativePidSend = false
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        if (packet.Length > maximumBytes || packet.IsEmpty || packet[0] != DistributionPassThroughLayout.Marker)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        options ??= new EtfDecodeOptions();
        var control = ReadTerm(packet[DistributionPassThroughLayout.MarkerBytes..], options, maximumBytes);
        if (control.Term is not TupleTerm tuple || tuple.Items.Count == 0 ||
            tuple.Items[DistributionControlOpcodes.OpcodeIndex] is not Integer opcode)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        if (opcode.Value < int.MinValue || opcode.Value > int.MaxValue ||
            !DistributionControlOpcodes.IsKnown((int)opcode.Value))
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        int payloadOffset = checked(DistributionPassThroughLayout.MarkerBytes + control.Used);
        bool link = opcode.Value == DistributionControlOpcodes.Link;
        bool exit = opcode.Value == DistributionControlOpcodes.Exit || opcode.Value == DistributionControlOpcodes.Exit2 ||
            opcode.Value == DistributionControlOpcodes.ExitTrace || opcode.Value == DistributionControlOpcodes.Exit2Trace;
        bool unlink = opcode.Value == DistributionControlOpcodes.UnlinkId || opcode.Value == DistributionControlOpcodes.UnlinkIdAck;
        bool monitor = opcode.Value == DistributionControlOpcodes.Monitor ||
            opcode.Value == DistributionControlOpcodes.Demonitor || opcode.Value == DistributionControlOpcodes.MonitorExit;
        if (opcode.Value == DistributionControlOpcodes.GroupLeader)
        {
            if (!IsGroupLeaderControl(tuple) || payloadOffset != packet.Length)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            ValidateFeatures(tuple, negotiatedFlags);

            return new(tuple, null);
        }
        if (monitor)
        {
            if (!IsMonitorControl(tuple) || payloadOffset != packet.Length)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            ValidateFeatures(tuple, negotiatedFlags);

            return new(tuple, null);
        }
        if (link || exit || unlink)
        {
            bool valid = link || exit
                ? tuple.Items.Count == (link ? DistributionControlOpcodes.LinkArity :
                    opcode.Value == DistributionControlOpcodes.ExitTrace || opcode.Value == DistributionControlOpcodes.Exit2Trace
                    ? DistributionControlOpcodes.ExitTraceArity : DistributionControlOpcodes.ExitArity) &&
                    tuple.Items[DistributionControlOpcodes.FromIndex] is Pid &&
                    tuple.Items[DistributionControlOpcodes.TargetIndex] is Pid
                : tuple.Items.Count == DistributionControlOpcodes.UnlinkArity &&
                    tuple.Items[DistributionControlOpcodes.UnlinkIdIndex] is Integer id &&
                    id.Value >= (opcode.Value == DistributionControlOpcodes.UnlinkId ? 1 : 0) && id.Value <= ulong.MaxValue &&
                    tuple.Items[DistributionControlOpcodes.UnlinkFromIndex] is Pid &&
                    tuple.Items[DistributionControlOpcodes.UnlinkTargetIndex] is Pid;
            if (!valid || payloadOffset != packet.Length)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            ValidateFeatures(tuple, negotiatedFlags);

            return new(tuple, null);
        }
        if (opcode.Value == DistributionControlOpcodes.PayloadExit || opcode.Value == DistributionControlOpcodes.PayloadMonitorExit)
        {
            bool monitorPayload = opcode.Value == DistributionControlOpcodes.PayloadMonitorExit;
            if (tuple.Items.Count != (monitorPayload ? DistributionControlOpcodes.MonitorArity : DistributionControlOpcodes.PayloadExitArity) ||
                (monitorPayload ? tuple.Items[DistributionControlOpcodes.FromIndex] is not (Pid or Atom)
                    : tuple.Items[DistributionControlOpcodes.FromIndex] is not Pid) ||
                tuple.Items[DistributionControlOpcodes.TargetIndex] is not Pid ||
                monitorPayload && tuple.Items[DistributionControlOpcodes.MonitorReferenceIndex] is not ReferenceTerm)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            var reason = ReadTerm(packet[payloadOffset..], options, maximumBytes);
            if (payloadOffset + reason.Used != packet.Length)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            ValidateFeatures(tuple, negotiatedFlags);
            ValidateFeatures(reason.Term, negotiatedFlags);
            unsupportedControlValidator?.Invoke(tuple);

            throw new NotSupportedException(DistributionPassThroughDiagnostics.UnsupportedControl);
        }
        ValidateControl(
            tuple,
            unsupportedControlValidator,
            negotiatedFlags,
            allowAlternativePidSend: allowAlternativePidSend
        );
        var payload = ReadTerm(packet[payloadOffset..], options, maximumBytes);
        if (payloadOffset + payload.Used != packet.Length)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
        ValidateFeatures(tuple, negotiatedFlags);
        ValidateFeatures(payload.Term, negotiatedFlags);

        return new(tuple, payload.Term);
    }

    public static byte[] EncodeProcessControl(
        TupleTerm control,
        int maximumBytes = ExternalTermFormat.DefaultMaxBytes,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        EtfEncodingOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ValidateFlags(negotiatedFlags);
        if (control.Items.Count == 0 || control.Items[DistributionControlOpcodes.OpcodeIndex] is not Integer opcode)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        bool valid = false;
        if (opcode.Value == DistributionControlOpcodes.Link)
            valid = control.Items.Count == DistributionControlOpcodes.LinkArity &&
                control.Items[DistributionControlOpcodes.FromIndex] is Pid &&
                control.Items[DistributionControlOpcodes.TargetIndex] is Pid;
        else if (opcode.Value == DistributionControlOpcodes.Exit || opcode.Value == DistributionControlOpcodes.Exit2 ||
            opcode.Value == DistributionControlOpcodes.ExitTrace || opcode.Value == DistributionControlOpcodes.Exit2Trace)
            valid = control.Items.Count == (opcode.Value == DistributionControlOpcodes.ExitTrace || opcode.Value == DistributionControlOpcodes.Exit2Trace
                ? DistributionControlOpcodes.ExitTraceArity : DistributionControlOpcodes.ExitArity) &&
                control.Items[DistributionControlOpcodes.FromIndex] is Pid &&
                control.Items[DistributionControlOpcodes.TargetIndex] is Pid;
        else if (opcode.Value == DistributionControlOpcodes.UnlinkId || opcode.Value == DistributionControlOpcodes.UnlinkIdAck)
            valid = control.Items.Count == DistributionControlOpcodes.UnlinkArity &&
                control.Items[DistributionControlOpcodes.UnlinkIdIndex] is Integer id &&
                id.Value >= (opcode.Value == DistributionControlOpcodes.UnlinkId ? 1 : 0) && id.Value <= ulong.MaxValue &&
                control.Items[DistributionControlOpcodes.UnlinkFromIndex] is Pid &&
                control.Items[DistributionControlOpcodes.UnlinkTargetIndex] is Pid;
        valid |= IsMonitorControl(control) || IsGroupLeaderControl(control);
        if (!valid)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        ValidateFeatures(control, negotiatedFlags);
        options ??= new EtfEncodingOptions();
        long required = checked(DistributionPassThroughLayout.MarkerBytes + ExternalTermFormat.EncodedSize(control, options));
        if (required > maximumBytes)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketLimit, nameof(control));
        byte[] encoded = ExternalTermFormat.Encode(control, options);
        int actualBytes = checked(DistributionPassThroughLayout.MarkerBytes + encoded.Length);
        if (actualBytes > maximumBytes)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketLimit, nameof(control));
        byte[] packet = new byte[actualBytes];
        packet[0] = DistributionPassThroughLayout.Marker;
        encoded.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes);

        return packet;
    }

    private static bool IsGroupLeaderControl(TupleTerm control)
    {
        var items = control.Items;

        return items.Count == DistributionControlOpcodes.GroupLeaderArity &&
            items[DistributionControlOpcodes.OpcodeIndex] is Integer opcode && opcode.Value == DistributionControlOpcodes.GroupLeader &&
            items[DistributionControlOpcodes.FromIndex] is Pid &&
            items[DistributionControlOpcodes.TargetIndex] is Pid;
    }

    private static bool IsMonitorControl(TupleTerm control)
    {
        var items = control.Items;
        if (items.Count < DistributionControlOpcodes.MonitorArity ||
            items[DistributionControlOpcodes.OpcodeIndex] is not Integer opcode)
            return false;
        bool down = opcode.Value == DistributionControlOpcodes.MonitorExit;
        bool request = opcode.Value == DistributionControlOpcodes.Monitor || opcode.Value == DistributionControlOpcodes.Demonitor;

        return (request || down) && items.Count == (down
            ? DistributionControlOpcodes.MonitorExitArity : DistributionControlOpcodes.MonitorArity) &&
            items[DistributionControlOpcodes.MonitorReferenceIndex] is ReferenceTerm &&
            (down
                ? items[DistributionControlOpcodes.FromIndex] is Pid or Atom && items[DistributionControlOpcodes.TargetIndex] is Pid
                : items[DistributionControlOpcodes.FromIndex] is Pid && items[DistributionControlOpcodes.TargetIndex] is Pid or Atom);
    }

    public static DistributionPassThroughMessage Send(Pid recipient, Term payload)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(payload);

        return new(
            Term.Tuple(
            Term.I(DistributionPassThroughLayout.SendOpcode),
            Term.A(DistributionPassThroughLayout.EmptyCookie),
            recipient
        ),
            payload
        );
    }

    public static DistributionPassThroughMessage SendRegistered(
        Pid sender,
        Atom recipient,
        Term payload
    )
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(payload);

        return new(
            Term.Tuple(
                Term.I(DistributionPassThroughLayout.RegisteredSendOpcode),
                sender,
                Term.A(DistributionPassThroughLayout.EmptyCookie),
                recipient
            ),
            payload
        );
    }

    private static EtfDecodedTerm ReadTerm(
        ReadOnlySpan<byte> bytes,
        EtfDecodeOptions options,
        int maximumBytes
    )
    {
        if (bytes.Length < EtfHeaderLayout.MinimumPacketBytes ||
            bytes[EtfHeaderLayout.TagOffset] is EtfTags.Compressed or EtfTags.Local)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(bytes));

        return ExternalTermFormat.DecodeOne(bytes, options, maximumBytes);
    }

    private static void ValidateFlags(ulong flags)
    {
        if ((flags & DistributionPassThroughFlags.Mandatory) != DistributionPassThroughFlags.Mandatory ||
            (flags & DistributionPassThroughFlags.Unsupported) != 0)
            throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
    }

    private static void ValidateFeatures(Term term, ulong flags)
    {
        if ((flags & DistributionPassThroughFlags.NativeRecords) != 0)
            return;
        var pending = new Stack<Term>();
        pending.Push(term);
        while (pending.TryPop(out var current))
        {
            switch (current)
            {
                case NativeRecordTerm:
                    throw new NotSupportedException(DistributionPassThroughDiagnostics.NativeRecords);
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (var item in tuple.Items)
                        pending.Push(item);
                    break;
                case MapTerm map:
                    foreach (var entry in map.Entries)
                    {
                        pending.Push(entry.Key);
                        pending.Push(entry.Value);
                    }
                    break;
                case FunctionTerm function:
                    foreach (var item in function.Environment)
                        pending.Push(item);
                    break;
            }
        }
    }

    private static void ValidateControl(
        TupleTerm control,
        Action<TupleTerm>? unsupportedControlValidator = null,
        ulong negotiatedFlags = DistributionPassThroughFlags.Mandatory,
        bool requireSenderNegotiation = false,
        bool allowAlternativePidSend = false
    )
    {
        ArgumentNullException.ThrowIfNull(control);
        if (control.Items.Count == 0 || control.Items[DistributionPassThroughLayout.OpcodeIndex] is not Integer opcode)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        if (opcode.Value == DistributionControlOpcodes.AlternativeActionSend)
        {
            if (!allowAlternativePidSend)
                throw new NotSupportedException(DistributionPassThroughDiagnostics.UnsupportedControl);
            ValidateAlternativePidSend(control, negotiatedFlags);
            unsupportedControlValidator?.Invoke(control);

            return;
        }
        if (opcode.Value == DistributionControlOpcodes.SendSender || opcode.Value == DistributionControlOpcodes.SendSenderTrace)
        {
            if (requireSenderNegotiation && (negotiatedFlags & DistributionPassThroughFlags.SendSender) == 0)
                throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
            if (control.Items.Count != (opcode.Value == DistributionControlOpcodes.SendSenderTrace
                ? DistributionControlOpcodes.SendTraceArity : DistributionPassThroughLayout.SendArity) ||
                control.Items[DistributionControlOpcodes.FromIndex] is not Pid ||
                control.Items[DistributionPassThroughLayout.SendTargetIndex] is not Pid)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));

            return;
        }
        if (opcode.Value == DistributionPassThroughLayout.SendOpcode || opcode.Value == DistributionControlOpcodes.SendTrace)
        {
            if (control.Items.Count != (opcode.Value == DistributionControlOpcodes.SendTrace
                ? DistributionControlOpcodes.SendTraceArity : DistributionPassThroughLayout.SendArity) ||
                control.Items[DistributionPassThroughLayout.SendTargetIndex] is not Pid)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));

            return;
        }
        if (opcode.Value == DistributionPassThroughLayout.RegisteredSendOpcode || opcode.Value == DistributionControlOpcodes.RegisteredSendTrace)
        {
            if (control.Items.Count != (opcode.Value == DistributionControlOpcodes.RegisteredSendTrace
                ? DistributionControlOpcodes.RegisteredSendTraceArity : DistributionPassThroughLayout.RegisteredSendArity) ||
                control.Items[DistributionPassThroughLayout.RegisteredSenderIndex] is not Pid ||
                control.Items[DistributionPassThroughLayout.RegisteredTargetIndex] is not Atom)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));

            return;
        }

        unsupportedControlValidator?.Invoke(control);

        throw new NotSupportedException(DistributionPassThroughDiagnostics.UnsupportedControl);
    }
}
