namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<bool> SendConnectedPidExitAsync(
        ProcessContext producer,
        Pid remote,
        Term reason,
        bool priority
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        uint connectionId;
        bool asynchronous;
        LocalNodeSnapshot snapshot;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        lock (gate)
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(producer);
            if (remote.LocalIdentity is not null && !ReferenceEquals(remote.LocalIdentity, runtime.LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (!entries.TryGetValue(remote.Node, out var weak) || !weak.TryGetTarget(out var found) ||
                found.Connected is not { } active || !IsConnectedSignalGeneration(found, active, found.ConnectionId))
                return false;
            entry = found;
            connected = active;
            connectionId = found.ConnectionId;
            asynchronous = producer.AsyncDistribution;
            snapshot = runtime.LocalIdentity.Snapshot;
            resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                ticket = ReserveOwnedOutput(connected, asynchronous ? new object() : producer);
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        HostDistributionStagedOutput? staged = null;
        AsyncDistributionOutput? output = null;
        Task? worker = null;
        try
        {
            if (System.Threading.Monitor.IsEntered(gate))
                await Task.Yield();

            HostDistributionBuffer Encode() => new([DistributionPassThroughCodec.EncodeExplicitPidExit(
                producer.Self,
                remote,
                reason,
                priority,
                connected.Flags,
                new EtfEncodingOptions
                {
                    NodeIdentity = runtime.LocalIdentity,
                    NodeSnapshot = snapshot,
                    FunctionOwner = producer.Self
                }
            ).AsMemory()]);
            if (!asynchronous)
            {
                await PublishOwnedPacket(
                    entry,
                    connected,
                    connectionId,
                    ticket,
                    ValidateProducer,
                    Encode,
                    producer.Cancellation,
                    waitingProducer: producer
                ).ConfigureAwait(false);

                return true;
            }
            lock (gate)
                ValidateProducer();
            HostDistributionBuffer packet = Encode();
            lock (gate)
            {
                ValidateProducer();
                staged = connected.Queue.TryStageOutput(connected.QueueHandle, packet)
                    ?? throw new ErlangException(ErlangErrorReasons.SystemLimit);
                output = new(
                    entry,
                    connected,
                    connectionId,
                    ticket,
                    resource,
                    staged
                );
                worker = PublishAsyncDistributionOutput(output);
                // Explicit public exits neither mutate sequential tokens nor produce ordinary send tracing/call markers.
                output.Acceptance.TrySetResult(true);

                return true;
            }
        }
        catch (OperationCanceledException) when (worker is null && !CapturedOutputGenerationIsCurrent(entry, connected, connectionId))
        {
            return true;
        }
        catch (Exception primary)
        {
            if (worker is not null)
            {
                output!.Acceptance.TrySetResult(false);
                try
                {
                    await worker.ConfigureAwait(false);
                }
                catch (Exception settlement)
                {
                    throw new AggregateException(primary, settlement);
                }
            }
            throw;
        }
        finally
        {
            if (worker is null)
            {
                try
                {
                    staged?.Dispose();
                }
                finally
                {
                    resource.Dispose();
                    ReleaseOwnedOutput(connected, ticket);
                }
            }
        }

        void ValidateProducer()
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
            RequireProcess(producer);
        }
    }
}
