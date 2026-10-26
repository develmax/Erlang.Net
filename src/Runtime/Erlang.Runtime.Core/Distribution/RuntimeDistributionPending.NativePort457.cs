namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal Term CreateNativePortChannel457(
        ProcessContext caller,
        Atom node,
        PortTerm port,
        ProcessContext kernel,
        ulong flags,
        uint creation
    )
    {
        lock (gate)
        {
            if (closed || !pending.TryGetValue(node.Name, out var entry)
                || entry.State != DistributionPendingState.Pending || entry.Connected is not null || entry.ReservedController is not null)
                return Term.A(DistChannel429Names.BadArgument);
            ResourceReferenceTerm? handle = null;
            NativeDistributionBinding457? binding = null;
            HostDistribution? queue = null;
            try
            {
                binding = runtime.BindNativeDistribution457(port, entry);
                queue = new HostDistribution(
                    runtime,
                    DistributionConnectedParameters.MaximumQueuedPacketBytes,
                    DistributionConnectedParameters.MaximumQueuedPackets,
                    DistributionConnectedParameters.MaximumQueuedSegments
                );
                var queueHandle = queue.AttachNative457(entry.Node.Name, binding);
                handle = runtime.ReferenceResources.CreateConnectedDistributionHandle(entry);
                var connected = new DistributionConnectedEntry(
                    queue,
                    queueHandle,
                    null,
                    flags,
                    creation
                )
                { NativePort457 = binding };
                entry.Connected = connected;
                entry.Identity.Creation = creation;
                entry.State = DistributionPendingState.Connected;
                binding.Start();
                runtime.SendSystem(kernel.Self, Term.Tuple(
                    Term.A(DistributionConnectedNames.ControllerNotice),
                    port,
                    entry.Node,
                    caller.Self
                ));
                NotifyConnectedNode(entry, connected, RuntimeLocalNodeAdmissionNames.NodeUp);

                return Term.Tuple(Term.A(DistChannel429Names.Ok), Term.Tuple(Term.I(entry.ConnectionId), handle));
            }
            catch (ErlangException)
            {
                if (entry.Connected is not null) RetirePending(entry);
                else
                {
                    queue?.Dispose();
                    binding?.Retire();
                }
                handle?.Entry.Close();

                return Term.A(DistChannel429Names.BadArgument);
            }
            catch
            {
                if (entry.Connected is not null) RetirePending(entry);
                else
                {
                    queue?.Dispose();
                    binding?.Retire();
                }
                handle?.Entry.Close();
                throw;
            }
        }
    }

    private void RequirePacketInput457(DistributionConnectedEntry connected, ProcessContext? caller, NativeDistributionBinding457? binding)
    {
        if (binding is null)
        {
            RequireInputHandler(connected, caller ?? throw new InvalidOperationException(HostDistributionDiagnostics.InputHandler));

            return;
        }
        if (caller is not null || !binding.IsLive || binding.Entry.ConnectionId != binding.Generation || !ReferenceEquals(connected.NativePort457, binding))
            throw new InvalidOperationException(HostDistributionDiagnostics.InputHandler);
    }

    private static void RecordPacketInput457(DistributionConnectedEntry connected, ProcessContext? caller, NativeDistributionBinding457? binding)
    {
        if (binding is null) connected.Queue.RecordAcceptedHostInput(connected.QueueHandle, caller!);
        else connected.Queue.RecordNativeInput457(connected.QueueHandle, binding);
    }

    internal void AcceptNativePort457(NativeDistributionBinding457 binding, ReadOnlySpan<byte> packet)
    {
        lock (gate)
        {
            if (!binding.IsLive || binding.Entry.ConnectionId != binding.Generation
                || binding.Entry.Connected is not { } connected || !ReferenceEquals(connected.NativePort457, binding)) return;
            connected.RawInputAttempts = unchecked(connected.RawInputAttempts + 1);
            if (packet.IsEmpty) return; // real depacketized distribution tick.
        }
        using var transfer = BeginOutputTransfer();
        try
        {
            AcceptRawConnectedProcessPacket(
                Term.Tuple(Term.I(binding.Generation), binding.Entry.Reference),
                null,
                packet,
                binding
            );
        }
        catch (InvalidOperationException) when (!binding.IsLive) { }
        catch (Exception failure) when (failure is ArgumentException or EndOfStreamException or InvalidDataException or FormatException or OverflowException or ErlangException)
        {
            RetireNativePort457(binding, Term.A(DistributionConnectionReasons.Closed));
        }
    }

    internal void RetireNativePort457(NativeDistributionBinding457 binding, Term reason)
    {
        lock (gate)
        {
            if (binding.Entry.ConnectionId == binding.Generation && binding.Entry.Connected is { } connected
                && ReferenceEquals(connected.NativePort457, binding)) RetirePending(binding.Entry, ControllerNodeDownReason(reason));
        }
    }
}
