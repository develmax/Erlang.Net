namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<DistributionGuestSendResult> SendConnectedPidAsync(
        ProcessContext producer,
        Pid remote,
        Term payload,
        bool priority,
        bool noSuspend
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        LocalNodeSnapshot snapshot;
        uint connectionId;
        SequenceTraceSendPreview preview = default;
        SequenceTraceToken? token;
        lock (gate)
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(producer);
            if (remote.LocalIdentity is not null && !ReferenceEquals(remote.LocalIdentity, runtime.LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (remote.Node == runtime.Node || !entries.TryGetValue(remote.Node, out var weak) ||
                !weak.TryGetTarget(out var found) || found.Connected is not { } active ||
                !IsConnectedSignalGeneration(found, active, found.ConnectionId))
                return DistributionGuestSendResult.NotConnected;
            entry = found;
            connected = active;
            connectionId = entry.ConnectionId;
            resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                ticket = ReserveOwnedOutput(connected, producer, immediate: noSuspend);
            }
            catch (DistributionOutputWouldBlockException) when (noSuspend)
            {
                resource.Dispose();

                return DistributionGuestSendResult.WouldSuspend;
            }
            catch
            {
                resource.Dispose();
                throw;
            }
            snapshot = runtime.LocalIdentity.Snapshot;
            token = null;
        }
        using (resource)
        {
            try
            {
                lock (gate)
                {
                    ValidateProducer();
                    if (noSuspend)
                    {
                        preview = runtime.SequenceTracing.PreviewSent(producer);
                        token = preview.Next;
                    }
                    else
                        token = runtime.SequenceTracing.Sent(producer, remote, payload);
                }
                var message = GuestPidMessage(
                    connected.Flags,
                    producer.Self,
                    remote,
                    payload,
                    priority,
                    token
                );

                HostDistributionBuffer Encode() => new([DistributionPassThroughCodec.Encode(
                    message,
                    negotiatedFlags: connected.Flags,
                    options: new EtfEncodingOptions
                    {
                        NodeIdentity = runtime.LocalIdentity,
                        NodeSnapshot = snapshot,
                        FunctionOwner = producer.Self
                    }
                ).AsMemory()]);
                if (noSuspend)
                {
                    HostDistributionBuffer packet = Encode();
                    lock (gate)
                    {
                        ValidateProducer();
                        if (!runtime.SequenceTracing.IsCurrentSendPreview(producer, preview) ||
                            !connected.Queue.WaitForOutputCapacity(connected.QueueHandle, packet).IsCompletedSuccessfully)
                            return DistributionGuestSendResult.WouldSuspend;
                        _ = runtime.SequenceTracing.Sent(producer, remote, payload);
                        if (!connected.Queue.TryEnqueueFinalizedHostBuffer(connected.QueueHandle, packet))
                            throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
                    }
                }
                else
                {
                    using var stop = CancellationTokenSource.CreateLinkedTokenSource(producer.Cancellation);
                    await PublishOwnedPacket(
                        entry,
                        connected,
                        connectionId,
                        ticket,
                        () =>
                        {
                            producer.Cancellation.ThrowIfCancellationRequested();
                            RequireProcess(producer);
                        },
                        Encode,
                        stop.Token
                    ).ConfigureAwait(false);
                }
                RecordAccepted();

                return DistributionGuestSendResult.Accepted;
            }
            catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(entry, connected, connectionId))
            {
                RecordAccepted();

                return DistributionGuestSendResult.Accepted;
            }
            finally
            {
                ReleaseOwnedOutput(connected, ticket);
            }
        }

        void ValidateProducer()
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
            RequireProcess(producer);
        }

        void RecordAccepted()
        {
            lock (gate)
            {
                runtime.ProcessTracing.Sent(
                    producer,
                    remote,
                    payload,
                    exists: true
                );
                producer.SavedCalls.Sent();
            }
        }
    }

    private static DistributionPassThroughMessage GuestPidMessage(
        ulong flags,
        Pid sender,
        Pid remote,
        Term payload,
        bool priority,
        SequenceTraceToken? token
    )
    {
        bool sendToken = token is not null && ((flags & DistributionPassThroughFlags.BigSequenceTraceLabels) != 0 ||
            token.Label is Integer label && label.Value >= DistributionGuestSendLayout.MinimumLegacyTraceLabel &&
            label.Value <= DistributionGuestSendLayout.MaximumLegacyTraceLabel);
        if (priority && (flags & DistributionPassThroughFlags.AlternativeActionSignal) != 0)
        {
            int actionFlags = DistributionAlternativePidSendLayout.Priority |
                (sendToken ? DistributionAlternativePidSendLayout.Token : 0);

            return new(
                sendToken
                ? Term.Tuple(
                    Term.I(DistributionControlOpcodes.AlternativeActionSend),
                    Term.I(actionFlags),
                    sender,
                    remote,
                    token!.ToTerm()
                )
                : Term.Tuple(
                    Term.I(DistributionControlOpcodes.AlternativeActionSend),
                    Term.I(actionFlags),
                    sender,
                    remote
                ),
                payload
            );
        }
        bool includeSender = (flags & DistributionPassThroughFlags.SendSender) != 0;
        int opcode = includeSender
            ? sendToken ? DistributionControlOpcodes.SendSenderTrace : DistributionControlOpcodes.SendSender
            : sendToken ? DistributionControlOpcodes.SendTrace : DistributionControlOpcodes.Send;
        Term identity = includeSender ? sender : Term.A(DistributionPassThroughLayout.EmptyCookie);

        return new(
            sendToken
            ? Term.Tuple(
                Term.I(opcode),
                identity,
                remote,
                token!.ToTerm()
            )
            : Term.Tuple(Term.I(opcode), identity, remote),
            payload
        );
    }
}
