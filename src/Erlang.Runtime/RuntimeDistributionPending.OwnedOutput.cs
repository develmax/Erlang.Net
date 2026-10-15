namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly HashSet<TaskCompletionSource> ownedOutputOperations = new();
    private readonly List<Exception> ownedOutputFailures = new();

    private void CompleteOwnedOutputOperation(TaskCompletionSource completion, Exception? failure = null)
    {
        lock (gate)
        {
            if (failure is not null)
                ownedOutputFailures.Add(failure);
            completion.TrySetResult();
            ownedOutputOperations.Remove(completion);
        }
    }

    private DistributionOutputTicket ReserveOwnedOutput(DistributionConnectedEntry connected, object producer, bool immediate = false)
    {
        lock (gate)
        {
            var ticket = immediate
                ? connected.Output.TryReserveImmediate(producer) ?? throw new DistributionOutputWouldBlockException()
                : connected.Output.Reserve(producer);
            ownedOutputOperations.Add(ticket.Completion);

            return ticket;
        }
    }

    private void ReleaseOwnedOutput(DistributionConnectedEntry connected, DistributionOutputTicket ticket, Exception? failure = null)
    {
        try
        {
            connected.Output.Abandon(ticket);
        }
        finally
        {
            CompleteOwnedOutputOperation(ticket.Completion, failure);
        }
    }

    internal async Task DrainOwnedOutputOperations()
    {
        while (true)
        {
            Task[] current;
            lock (gate)
            {
                current = ownedOutputOperations.Select(item => item.Task).ToArray();
                if (current.Length == 0)
                {
                    if (ownedOutputFailures.Count != 0)
                        throw new AggregateException(ownedOutputFailures);

                    return;
                }
            }
            await Task.WhenAll(current).ConfigureAwait(false);
        }
    }

    internal async ValueTask<bool> PublishLiveControl(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        ProcessContext producer,
        TupleTerm control,
        Pid sender,
        CancellationToken cancellation = default,
        Func<bool>? stateValid = null,
        Action? commit = null
    )
    {
        DistributionOutputTicket ticket;
        LocalNodeSnapshot snapshot;
        ReferenceResourceLease<RuntimeDistributionPendingEntry>? resource;
        lock (gate)
        {
            RequireProcess(producer);
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                return false;
            resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference);
            if (resource is null)
                throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                snapshot = runtime.LocalIdentity.Snapshot;
                ticket = ReserveOwnedOutput(connected, producer);
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
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation, producer.Cancellation);
                await PublishOwnedPacket(
                    entry,
                    connected,
                    connectionId,
                    ticket,
                    () =>
                    {
                        producer.Cancellation.ThrowIfCancellationRequested();
                        RequireProcess(producer);
                        if (stateValid is not null && !stateValid())
                            throw new DistributionOutputStateChangedException();
                    },
                    () => new HostDistributionBuffer([DistributionPassThroughCodec.EncodeProcessControl(
                        control,
                        negotiatedFlags: connected.Flags,
                        options: new EtfEncodingOptions
                        {
                            NodeIdentity = runtime.LocalIdentity,
                            NodeSnapshot = snapshot,
                            FunctionOwner = sender
                        }
                    ).AsMemory()]),
                    stop.Token,
                    commit
                ).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(entry, connected, connectionId))
            {
                return false;
            }
            catch (DistributionOutputStateChangedException)
            {
                return false;
            }
            finally
            {
                ReleaseOwnedOutput(connected, ticket);
            }
        }
    }

    public async ValueTask EnqueueConnectedOutputAsync(
        Term handle,
        ProcessContext sender,
        DistributionPassThroughMessage message,
        CancellationToken cancellation = default
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        DistributionOutputTicket ticket;
        uint connectionId;
        LocalNodeSnapshot snapshot;
        lock (gate)
        {
            entry = ReadHandle(handle, out connectionId, out resource);
            try
            {
                RequireProcess(sender);
                connected = RequireConnected(entry, connectionId);
                snapshot = runtime.LocalIdentity.Snapshot;
                ticket = ReserveOwnedOutput(connected, sender);
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
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation, sender.Cancellation);
                await PublishOwnedPacket(
                    entry,
                    connected,
                    connectionId,
                    ticket,
                    () => RequireProcess(sender),
                    () => new HostDistributionBuffer([DistributionPassThroughCodec.Encode(
                        message,
                        negotiatedFlags: connected.Flags,
                        options: new EtfEncodingOptions
                        {
                            NodeIdentity = runtime.LocalIdentity,
                            NodeSnapshot = snapshot,
                            FunctionOwner = sender.Self
                        }
                    ).AsMemory()]),
                    stop.Token
                ).ConfigureAwait(false);
            }
            finally
            {
                ReleaseOwnedOutput(connected, ticket);
            }
        }
    }

    internal async ValueTask<bool> PublishTerminalControl(DistributionExitOutput output, ProcessExitContinuation owner)
    {
        DistributionOutputTicket ticket;
        LocalNodeSnapshot snapshot;
        ReferenceResourceLease<RuntimeDistributionPendingEntry>? resource;
        lock (gate)
        {
            if (!runtime.OwnsExitContinuation(owner))
                throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.InvalidExitOwner);
            if (!IsConnectedSignalGeneration(output.Entry, output.Connection, output.ConnectionId))
                return false;
            resource = runtime.ReferenceResources.AcquireDistributionEntry(output.Entry.Reference);
            if (resource is null)
                throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                snapshot = runtime.LocalIdentity.Snapshot;
                ticket = ReserveOwnedOutput(output.Connection, owner);
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
                await PublishOwnedPacket(
                    output.Entry,
                    output.Connection,
                    output.ConnectionId,
                    ticket,
                    () =>
                    {
                        if (!runtime.OwnsExitContinuation(owner))
                            throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.InvalidExitOwner);
                    },
                    () => new HostDistributionBuffer([DistributionPassThroughCodec.EncodeProcessControl(
                        output.Control,
                        negotiatedFlags: output.Connection.Flags,
                        options: new EtfEncodingOptions
                        {
                            NodeIdentity = runtime.LocalIdentity,
                            NodeSnapshot = snapshot,
                            FunctionOwner = output.Sender
                        }
                    ).AsMemory()]),
                    CancellationToken.None
                ).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(output.Entry, output.Connection, output.ConnectionId))
            {
                return false;
            }
            catch (Exception exception)
            {
                try
                {
                    RetireFailedOutputGeneration(output.Entry, output.Connection, output.ConnectionId);
                }
                catch (Exception retirementFailure)
                {
                    throw new AggregateException(exception, retirementFailure);
                }
                throw;
            }
            finally
            {
                ReleaseOwnedOutput(output.Connection, ticket);
            }
        }
    }

    private bool CapturedOutputGenerationIsCurrent(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, uint connectionId)
    {
        lock (gate)
            return IsConnectedSignalGeneration(entry, connected, connectionId);
    }

    private async ValueTask PublishOwnedPacket(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        DistributionOutputTicket ticket,
        Action validateProducer,
        Func<HostDistributionBuffer> encode,
        CancellationToken cancellation,
        Action? commit = null
    )
    {
        await connected.Output.Publish(
            ticket,
            () =>
            {
                lock (gate)
                    Validate();

                return encode();
            },
            packet =>
            {
                lock (gate)
                {
                    Validate();

                    if (!connected.Queue.TryEnqueueFinalizedHostBuffer(connected.QueueHandle, packet))
                        return false;
                    commit?.Invoke();

                    return true;
                }
            },
            packet =>
            {
                lock (gate)
                {
                    Validate();

                    return connected.Queue.WaitForOutputCapacity(connected.QueueHandle, packet);
                }
            },
            cancellation,
            deferStart: System.Threading.Monitor.IsEntered(gate)
        ).ConfigureAwait(false);

        void Validate()
        {
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
            validateProducer();
        }
    }
}
