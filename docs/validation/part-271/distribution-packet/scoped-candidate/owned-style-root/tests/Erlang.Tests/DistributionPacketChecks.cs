namespace Erlang.Tests;

internal static class DistributionPacketChecks
{
    internal static Task Run()
    {
        var recipient = new Pid(
            DistributionPacketTestNames.RecipientNode,
            DistributionPacketTestParameters.RecipientId,
            DistributionPacketTestParameters.RecipientCreation
        );
        var sender = new Pid(
            DistributionPacketTestNames.SenderNode,
            DistributionPacketTestParameters.SenderId,
            DistributionPacketTestParameters.SenderCreation
        );
        var raw = new byte[] { 0, 128, 255 };
        var payload = Term.Tuple(
            Term.A(DistributionPacketTestNames.Payload),
            new BitString(raw),
            new Cons(Term.A(DistributionPacketTestNames.Head), new BitString(raw))
        );
        var messages = new[]
        {
            DistributionPassThroughCodec.Send(recipient, payload),
            DistributionPassThroughCodec.SendRegistered(sender, Term.A(DistributionPacketTestNames.Recipient), payload),
            new DistributionPassThroughMessage(
                Term.Tuple(
                    Term.I(DistributionPassThroughLayout.RegisteredSendOpcode),
                    sender,
                    Term.I(DistributionPacketTestParameters.IgnoredRegisteredCookie),
                    Term.A(DistributionPacketTestNames.Recipient)
                ),
                payload
            )
        };
        foreach (var message in messages)
        {
            byte[] packet = DistributionPassThroughCodec.Encode(message);
            byte[] control = ExternalTermFormat.Encode(message.Control);
            byte[] body = ExternalTermFormat.Encode(message.Payload);
            Check(packet.SequenceEqual(new byte[] { DistributionPassThroughLayout.Marker }.Concat(control).Concat(body)));
            var restored = DistributionPassThroughCodec.Decode(packet);
            Check(restored.Control.Equals(message.Control) && restored.Payload.Equals(message.Payload));
            Check(DistributionPassThroughCodec.Encode(message, packet.Length).SequenceEqual(packet));
            Reject(() => DistributionPassThroughCodec.Encode(message, packet.Length - 1));
            Reject(() => DistributionPassThroughCodec.Decode(packet, maximumBytes: packet.Length - 1));
            for (int prefix = 0; prefix < packet.Length; prefix++)
            {
                byte[] truncated = packet[..prefix];
                Reject(() => DistributionPassThroughCodec.Decode(truncated));
            }
            Reject(() => DistributionPassThroughCodec.Decode(packet.Concat(new byte[] { 0 }).ToArray()));
            packet[0] = DistributionPacketTestParameters.WrongMarker;
            Reject(() => DistributionPassThroughCodec.Decode(packet));
        }
        var golden = DistributionPassThroughCodec.Send(
            new Pid(DistributionPacketTestNames.GoldenNode, DistributionPacketTestParameters.GoldenPidId),
            Term.I(DistributionPacketTestParameters.GoldenPayload)
        );
        byte[] goldenBytes = DistributionPassThroughCodec.Encode(golden);
        Check(goldenBytes.AsSpan().SequenceEqual(DistributionPacketTestParameters.GoldenSend));
        var goldenDecoded = DistributionPassThroughCodec.Decode(DistributionPacketTestParameters.GoldenSend);
        Check(goldenDecoded.Control.Equals(golden.Control) && goldenDecoded.Payload.Equals(golden.Payload));
        foreach (var flag in new[] { DistributionPassThroughFlags.AtomCacheHeader, DistributionPassThroughFlags.Fragments, DistributionPassThroughFlags.SendSender })
        {
            ulong flags = DistributionPassThroughFlags.Mandatory | flag;
            Reject(() => DistributionPassThroughCodec.Encode(messages[0], negotiatedFlags: flags));
            Reject(() => DistributionPassThroughCodec.Decode(goldenBytes, negotiatedFlags: flags));
        }
        Reject(() => DistributionPassThroughCodec.Encode(messages[0], negotiatedFlags: 0));
        Reject(() => DistributionPassThroughCodec.Decode(goldenBytes, negotiatedFlags: 0));
        Reject(() => DistributionPassThroughCodec.Encode(messages[0], maximumBytes: 0));
        Reject(() => DistributionPassThroughCodec.Decode(goldenBytes, maximumBytes: 0));
        foreach (byte envelope in new[] { DistributionPacketTestParameters.CompressedEnvelope, DistributionPacketTestParameters.LocalEnvelope })
        {
            var invalid = goldenBytes.ToArray();
            invalid[DistributionPacketTestParameters.ControlTagOffset] = envelope;
            Reject(() => DistributionPassThroughCodec.Decode(invalid));
        }
        var malformed = new[]
        {
            Term.Tuple(Term.I(DistributionPassThroughLayout.SendOpcode)),
            Term.Tuple(
                Term.I(DistributionPassThroughLayout.SendOpcode),
                Term.A(DistributionPacketTestNames.EmptyCookie),
                Term.A(DistributionPacketTestNames.Recipient)
            ),
            Term.Tuple(
                Term.I(DistributionPassThroughLayout.RegisteredSendOpcode),
                Term.I(1),
                Term.A(DistributionPacketTestNames.EmptyCookie),
                Term.A(DistributionPacketTestNames.Recipient)
            ),
            Term.Tuple(
                Term.I(DistributionPassThroughLayout.RegisteredSendOpcode),
                sender,
                Term.A(DistributionPacketTestNames.EmptyCookie),
                recipient
            ),
            Term.Tuple(Term.I(DistributionPacketTestParameters.UnsupportedOpcode), sender, recipient),
            Term.Tuple(Term.A(DistributionPacketTestNames.Payload), recipient)
        };
        foreach (var control in malformed)
        {
            Reject(() => DistributionPassThroughCodec.Encode(new(control, payload)));
            byte[] invalid = new byte[] { DistributionPassThroughLayout.Marker }
                .Concat(ExternalTermFormat.Encode(control)).Concat(ExternalTermFormat.Encode(payload)).ToArray();
            Reject(() => DistributionPassThroughCodec.Decode(invalid));
        }
        var record = new NativeRecordTerm(
            Term.A(DistributionPacketTestNames.Module),
            Term.A(DistributionPacketTestNames.Record),
            true,
            new[] { new KeyValuePair<Atom, Term>(Term.A(DistributionPacketTestNames.Field), payload) }
        );
        var nestedRecord = Term.Tuple(new MapTerm(new[]
        {
            new KeyValuePair<Term, Term>(Term.A(DistributionPacketTestNames.Field), new Cons(record, Nil.Value))
        }));
        var recordMessage = DistributionPassThroughCodec.Send(recipient, nestedRecord);
        Reject(() => DistributionPassThroughCodec.Encode(recordMessage));
        ulong recordFlags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.NativeRecords;
        byte[] recordPacket = DistributionPassThroughCodec.Encode(recordMessage, negotiatedFlags: recordFlags);
        Reject(() => DistributionPassThroughCodec.Decode(recordPacket));
        Check(DistributionPassThroughCodec.Decode(recordPacket, negotiatedFlags: recordFlags).Payload.Equals(nestedRecord));
        var encodedBeforeMutation = DistributionPassThroughCodec.Encode(messages[0]);
        raw[0] = byte.MaxValue;
        Check(DistributionPassThroughCodec.Encode(messages[0]).SequenceEqual(encodedBeforeMutation));

        return Task.CompletedTask;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionPacketTestNames.Failure);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is ArgumentException or ErlangException or NotSupportedException)
        {
            return;
        }

        throw new InvalidOperationException(DistributionPacketTestNames.Accepted);
    }
}
