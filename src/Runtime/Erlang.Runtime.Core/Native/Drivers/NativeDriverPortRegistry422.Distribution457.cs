namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal NativeDistributionBinding457 BindDistribution457(
        PortTerm identity,
        RuntimeDistributionPendingEntry entry,
        DriverParentPeerTransport422 transport
    )
    {
        lock (gate)
        {
            var row = Require(identity);
            if (!row.DriverSoftBusy || row.Distribution457 is not null || row.PhysicallyStopped)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var binding = new NativeDistributionBinding457(
                this,
                row,
                transport,
                entry
            );
            row.Distribution457 = binding;
            row.MessageLimitsEnabled = false;
            row.MessageBusy = false;
            row.CheckMessageBusy = false;
            if (!row.ManualBusy) row.Writable.TrySetResult();

            return binding;
        }
    }

    internal void StartDistribution457(NativeDistributionBinding457 binding, NativeDriverPortRow422 row, DriverParentPeerTransport422 transport)
    {
        lock (gate)
        {
            if (ExecutionContext.IsFlowSuppressed()) Track(Task.Run(() => PumpDistribution457(binding, row, transport)));
            else using (ExecutionContext.SuppressFlow()) Track(Task.Run(() => PumpDistribution457(binding, row, transport)));
        }
    }

    private async Task PumpDistribution457(NativeDistributionBinding457 binding, NativeDriverPortRow422 row, DriverParentPeerTransport422 transport)
    {
        try
        {
            while (true)
            {
                Task? wait;
                Task<Term>? output = null;
                lock (gate)
                {
                    if (!binding.IsLive || !ReferenceEquals(row.Distribution457, binding)) return;
                    var connected = binding.Entry.Connected;
                    if (connected is null || !ReferenceEquals(connected.NativePort457, binding)) return;
                    // Arm before inspecting the queue. A producer that wins
                    // either side of the empty check cannot lose its wakeup.
                    wait = binding.CaptureWakeBeforeQueueCheck();
                    var packet = connected.Queue.TakeNative457(connected.QueueHandle, binding);
                    if (packet is not null)
                    {
                        wait = null;
                        byte[] bytes = new byte[checked((int)packet.PayloadBytes)];
                        int offset = 0;
                        foreach (var segment in packet.Segments)
                        {
                            segment.ByteSpan.CopyTo(bytes.AsSpan(offset));
                            offset += segment.ByteLength;
                        }
                        output = EnqueueSignal454(
                            row,
                            null,
                            true,
                            async () =>
                        {
                            lock (gate)
                            {
                                if (!binding.IsLive || binding.Entry.ConnectionId != binding.Generation) return Nil.Value;
                                row.OutputBytes = checked(row.OutputBytes + bytes.LongLength);
                                runtime.Statistics.RecordOutputBytes(bytes.LongLength);
                            }
                            await transport.DistributionOutput457(row.Native, bytes).ConfigureAwait(false);

                            return Nil.Value;
                        }
                        );
                    }
                }
                if (output is not null) await output.ConfigureAwait(false);
                else await wait!.ConfigureAwait(false);
            }
        }
        catch (Exception failure)
        {
            binding.Entry.Owner.RetireNativePort457(binding, Term.A(DistributionConnectionReasons.Closed));
            if (failure is not (ErlangException or InvalidOperationException)) throw;
        }
    }

    internal void CloseDistribution457(NativeDistributionBinding457 binding, NativeDriverPortRow422 row, DriverParentPeerTransport422 transport)
    {
        lock (gate)
        {
            if (!ReferenceEquals(row.Distribution457, binding) || row.PhysicallyStopped || row.Closing || row.StopRequested) return;
            QueueCloseSignal(row.Identity, Term.A(DistributionConnectionReasons.Closed), transport);
        }
    }
}
