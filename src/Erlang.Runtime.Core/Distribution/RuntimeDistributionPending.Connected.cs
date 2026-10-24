namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    public TupleTerm AdoptConnected(
        Term handle,
        ProcessContext controller,
        ulong negotiatedFlags,
        uint remoteCreation
    )
    {
        lock (gate)
        {
            return AdoptConnectedOwned(
                handle,
                controller,
                negotiatedFlags,
                remoteCreation,
                runtime.RequireDistributionKernel(requireRegistered: true),
                controller.Self
            );
        }
    }

    private TupleTerm AdoptConnectedOwned(
        Term handle,
        ProcessContext controller,
        ulong negotiatedFlags,
        uint remoteCreation,
        ProcessContext kernel,
        Pid setupPid
    )
    {
        lock (gate)
        {
            RequireProcess(controller);
            if (!ReferenceEquals(runtime.RequireDistributionKernel(), kernel))
                throw new InvalidOperationException(DistributionConnectedDiagnostics.Kernel);
            if (!DistributionCapabilityProfile429.HasMandatory(negotiatedFlags) ||
                DistributionCapabilityProfile429.HasUnsupported(negotiatedFlags))
                throw new NotSupportedException(DistributionConnectedDiagnostics.Flags);
            if (exitingInputControllers.Contains(controller) ||
                controller.DistributionRole != RuntimeDistributionRole.None || ReferenceEquals(controller, kernel))
                throw new InvalidOperationException(DistributionConnectedDiagnostics.Controller);
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                ValidateEntry(entry, connectionId);
                if (entry.Node.Name == runtime.Node)
                    throw new ArgumentException(DistributionPendingDiagnostics.Node, nameof(handle));
                if (entry.State != DistributionPendingState.Pending || entry.Connected is not null ||
                    entry.ReservedController is not null && !ReferenceEquals(entry.ReservedController, controller) ||
                    controllers.TryGetValue(controller.Self, out var owned) && !ReferenceEquals(owned, entry))
                    throw new InvalidOperationException(DistributionConnectedDiagnostics.Pending);
                var queue = new HostDistribution(
                    runtime,
                    DistributionConnectedParameters.MaximumQueuedPacketBytes,
                    DistributionConnectedParameters.MaximumQueuedPackets,
                    DistributionConnectedParameters.MaximumQueuedSegments
                );
                HostDistributionHandle queueHandle;
                try
                {
                    queueHandle = queue.Attach(entry.Node.Name, controller);
                }
                catch
                {
                    queue.Dispose();
                    throw;
                }
                entry.Connected = new(
                    queue,
                    queueHandle,
                    controller,
                    negotiatedFlags,
                    remoteCreation
                );
                entry.Identity.Creation = remoteCreation;
                entry.ReservedController = controller;
                controllers[controller.Self] = entry;
                controller.DistributionRole = RuntimeDistributionRole.Controller;
                entry.State = DistributionPendingState.Connected;
                runtime.SendSystem(kernel.Self, Term.Tuple(
                    Term.A(DistributionConnectedNames.ControllerNotice),
                    controller.Self,
                    entry.Node,
                    setupPid
                ));
                NotifyConnectedNode(entry, entry.Connected, RuntimeLocalNodeAdmissionNames.NodeUp);

                return Term.Tuple(Term.I(connectionId), entry.Reference);
            }
        }
    }

    public DistributionConnectedSnapshot ConnectedSnapshot(Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);
                var snapshot = connected.Queue.Snapshot(connected.QueueHandle);

                return new(
                    entry.Node,
                    connectionId,
                    connected.Flags,
                    connected.RemoteCreation,
                    snapshot.Controller,
                    connected.InputHandler,
                    snapshot.GetSize,
                    snapshot.NotificationArmed,
                    snapshot.AcceptedInputPackets,
                    snapshot.ConsumedOutputPackets,
                    snapshot.QueuedPayloadBytes,
                    snapshot.QueuedBuffers,
                    snapshot.QueuedSegments,
                    connected.RawInputAttempts
                );
            }
        }
    }

    public bool SetConnectedGetSize(Term handle, ProcessContext caller, bool value)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);

                return connected.Queue.SetGetSize(connected.QueueHandle, caller, value);
            }
        }
    }

    public bool ConnectedGetSize(Term handle, ProcessContext caller)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);

                return connected.Queue.GetSize(connected.QueueHandle, caller);
            }
        }
    }

    public void SetConnectedInputHandler(Term handle, ProcessContext caller, Pid handler)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);
                if (handler.LocalIdentity is not null && !ReferenceEquals(handler.LocalIdentity, runtime.LocalIdentity))
                    throw new ArgumentException(DistributionConnectedDiagnostics.Controller, nameof(handler));
                connected.Queue.SetInputHandler(connected.QueueHandle, caller, handler);
                connected.InputHandler = handler;
            }
        }
    }

    public void RequestConnectedOutputNotification(Term handle, ProcessContext caller)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);
                connected.Queue.ArmNotification(connected.QueueHandle, caller);
            }
        }
    }

    public HostDistributionBuffer? TakeConnectedOutput(
        Term handle,
        ProcessContext caller,
        out bool getSize
    )
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);

                return connected.Queue.TakeFinalizedHostBuffer(connected.QueueHandle, caller, out getSize);
            }
        }
    }

    public void EnqueueConnectedOutput(Term handle, DistributionPassThroughMessage message)
        => EnqueueConnectedOutputOwned(handle, message, null);

    public void EnqueueConnectedOutput(
        Term handle,
        ProcessContext sender,
        DistributionPassThroughMessage message
    ) => EnqueueConnectedOutputOwned(handle, message, sender);

    private void EnqueueConnectedOutputOwned(
        Term handle,
        DistributionPassThroughMessage message,
        ProcessContext? sender
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        uint connectionId;
        EtfEncodingOptions options;
        lock (gate)
        {
            entry = ReadHandle(handle, out connectionId, out resource);
            try
            {
                connected = RequireConnected(entry, connectionId);
                if (sender is not null)
                    RequireProcess(sender);
                options = new EtfEncodingOptions
                {
                    NodeIdentity = runtime.LocalIdentity,
                    NodeSnapshot = runtime.LocalIdentity.Snapshot,
                    FunctionOwner = sender?.Self
                };
                ticket = ReserveOwnedOutput(connected, sender ?? new object(), immediate: true);
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        using (resource)
        {
            try
            {
                byte[] bytes = DistributionPassThroughCodec.Encode(message, negotiatedFlags: connected.Flags, options: options);
                var buffer = new HostDistributionBuffer([bytes.AsMemory()]);
                lock (gate)
                {
                    if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                        throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
                    if (sender is not null)
                        RequireProcess(sender);
                    if (!connected.Queue.TryEnqueueFinalizedHostBuffer(connected.QueueHandle, buffer))
                        throw new DistributionOutputWouldBlockException();
                }
            }
            finally
            {
                ReleaseOwnedOutput(connected, ticket);
            }
        }
    }

    public void AcceptConnectedPassThroughInput(
        Term handle,
        ProcessContext caller,
        ReadOnlySpan<byte> packet
    ) => AcceptConnectedInput(
        handle,
        caller,
        packet,
        false
    );

    private void AcceptConnectedInput(
        Term handle,
        ProcessContext caller,
        ReadOnlySpan<byte> packet,
        bool rawInput
    )
    {
        using var outputTransfer = BeginOutputTransfer();
        if (rawInput)
        {
            AcceptRawConnectedProcessPacket(handle, caller, packet);

            return;
        }
        RuntimeDistributionPendingEntry entry;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        uint connectionId;
        ulong flags;
        LocalNodeSnapshot wire;
        lock (gate)
        {
            entry = ReadHandle(handle, out connectionId, out resource);
            try
            {
                var connected = RequireConnected(entry, connectionId);
                RequireInputHandler(connected, caller);
                wire = runtime.LocalIdentity.Snapshot;
                flags = connected.Flags;
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        using (resource)
        {
            var options = new EtfDecodeOptions
            {
                ExternalFunctionFactory = (identity, requireSafe) => runtime.Functions.External(runtime, identity, requireSafe),
                LocalFunctionFactory = (code, arity, environment) => runtime.Functions.Restore(
                    runtime,
                    new LocalFunctionIdentity(
                        code.Module,
                        null,
                        code.Index,
                        code.OldUnique,
                        code.Unique,
                        code.OldIndex
                    ),
                    arity,
                    environment
                )
            };
            if (packet.Length > ExternalTermFormat.DefaultMaxBytes)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(packet));
            byte[] ownedPacket = packet.ToArray();
            var epochs = CollectConnectedPacketEpochs(ownedPacket, options, rawInput);
            using var admission = runtime.AdmitExternalDecode(options, epochs);
            lock (gate)
            {
                _ = RequireConnectedDecodeSnapshot(
                    entry,
                    connectionId,
                    caller,
                    wire
                );
            }
            var message = DistributionPassThroughCodec.Decode(
                ownedPacket,
                admission.Options,
                negotiatedFlags: flags,
                unsupportedControlValidator: rawInput
                    ? control => ValidateUnsupportedConnectedControl(control, entry.Node, wire)
                    : null
            );
            admission.Observe(Term.Tuple(message.Control, message.Payload));
            lock (gate)
            {
                var connected = RequireConnectedDecodeSnapshot(
                    entry,
                    connectionId,
                    caller,
                    wire
                );
                connected.Queue.RecordAcceptedHostInput(connected.QueueHandle, caller);
                if (DistributionPassThroughCodec.IsSpawnRequest(message.Control))
                    RouteConnectedSpawnRequest(
                        entry,
                        connected,
                        message.Control,
                        message.Payload
                    );
                else
                    DeliverConnectedTracePayload(message.Control, message.Payload);
            }
        }
    }

    private void RequireInputHandler(DistributionConnectedEntry connected, ProcessContext caller)
    {
        RequireProcess(caller);
        if (connected.InputHandler is not { } inputHandler || !inputHandler.Equals(caller.Self))
            throw new InvalidOperationException(HostDistributionDiagnostics.InputHandler);
    }

    public void CloseConnected(Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                _ = RequireConnected(entry, connectionId);
                RetirePending(entry);
            }
        }
    }

    private DistributionConnectedEntry RequireConnected(RuntimeDistributionPendingEntry entry, uint connectionId)
    {
        ValidateEntry(entry, connectionId);
        _ = runtime.RequireDistributionKernel();
        if (entry.State != DistributionPendingState.Connected || entry.Connected is not { } connected ||
            (connected.NativePort457 is { } port ? !port.IsLive : connected.Controller?.DistributionRole != RuntimeDistributionRole.Controller))
            throw new InvalidOperationException(DistributionConnectedDiagnostics.Connected);
        if (connected.Controller is { } controller) RequireProcess(controller);

        return connected;
    }

    internal bool HasDistributionHandle(Term handle)
    {
        lock (gate)
        {
            try
            {
                _ = ReadHandle(handle, out _, out var resource);
                resource.Dispose();

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    private void RetireConnected(RuntimeDistributionPendingEntry entry, Term? reason = null)
    {
        if (entry.Connected is not { } connected)
        {
            RetireControllerAdoptions(entry: entry);

            return;
        }
        // Reentrant internal signals must see a retired identity before any callbacks run.
        entry.Connected = null;
        entry.State = DistributionPendingState.Exiting;
        if (connected.Controller is { } controller) controller.DistributionRole = RuntimeDistributionRole.None;
        var failures = new List<Exception>();
        RetireStep(() => connected.NativePort457?.Retire());
        RetireStep(connected.Output.Dispose);
        RetireStep(connected.Queue.Dispose);
        RetireStep(() => RetireControllerAdoptions(entry: entry));
        RetireStep(() => runtime.RetireConnectedProcessLinks(entry, connected));
        RetireStep(() => runtime.RetireConnectedProcessMonitors(entry, connected));
        RetireStep(() => RetireConnectedSpawnRequests(entry, connected));
        RetireStep(() => RetireNodeMonitors458(entry));
        RetireStep(() => NotifyConnectedNode(
            entry,
            connected,
            RuntimeLocalNodeAdmissionNames.NodeDown,
            reason ?? Term.A(DistributionConnectionReasons.Closed)
        ));
        entry.State = DistributionPendingState.Idle;
        if (failures.Count != 0)
            throw new AggregateException(failures);

        void RetireStep(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }
}
