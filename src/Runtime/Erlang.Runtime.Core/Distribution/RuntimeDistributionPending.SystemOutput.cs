namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal void ScheduleConnectedControlSignal(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        TupleTerm control,
        Pid sender
    )
    {
        lock (gate)
        {
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                return;
            var resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                // Reserve logical order and pin the generation before returning accepted input.
                // No encoding or capacity wait runs in the input handler's call.
                var snapshot = runtime.LocalIdentity.Snapshot;
                var producer = new object();
                var ticket = ReserveOwnedOutput(connected, producer);
                Task previous = connected.SystemOutput;
                connected.SystemOutput = ticket.Completion.Task;
                Action start = () => _ = PublishSystemControl(
                    previous,
                    ticket,
                    resource,
                    entry,
                    connected,
                    connectionId,
                    control,
                    sender,
                    snapshot
                );
                if (!DistributionOutputTransferScope.TryAdd(this, start))
                    StartTransferredOutput(start);
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
    }

    private async Task PublishSystemControl(
        Task previous,
        DistributionOutputTicket ticket,
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource,
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        TupleTerm control,
        Pid sender,
        LocalNodeSnapshot snapshot
    )
    {
        Exception? failure = null;
        try
        {
            using (resource)
            {
                await previous.ConfigureAwait(false);
                lock (gate)
                {
                    if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                        return;
                }
                await PublishOwnedPacket(
                    entry,
                    connected,
                    connectionId,
                    ticket,
                    () => { },
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
                    CancellationToken.None
                ).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(entry, connected, connectionId))
        {
            // Retirement of this exact generation resolves an uncommitted reply.
        }
        catch (Exception exception)
        {
            failure = exception;
            try
            {
                RetireFailedOutputGeneration(entry, connected, connectionId);
            }
            catch (Exception retirementFailure)
            {
                failure = new AggregateException(exception, retirementFailure);
            }
        }
        finally
        {
            ReleaseOwnedOutput(connected, ticket, failure);
        }
    }

    internal void RetireFailedOutputGeneration(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId
    )
    {
        lock (gate)
        {
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                return;
            // Cancel waiters before potentially throwing node-down callbacks. Never touch a replacement.
            connected.Output.Dispose();
            if (connected.Controller is { } controller) exitingInputControllers.Add(controller);
            try
            {
                RetirePending(entry);
            }
            finally
            {
                if (connected.Controller is { } failedController) runtime.QueueDistributionControllerKill(failedController);
            }
        }
    }
}
