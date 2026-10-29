namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private async ValueTask<ReferenceTerm?> StartNativeDistSpawnAsynchronously430(
        ProcessContext caller,
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        SequenceSpawnDescription description,
        ProcessSpawnRequestOptions options,
        RuntimeProcessPayload641 prepared641
    )
    {
        var input641 = prepared641.Borrow641(caller);
        try
        {
            DistributionOutputTicket? ticket = null;
            ReferenceResourceLease<RuntimeDistributionPendingEntry>? resource = null;
            DistributionSpawnRequest? request = null;
            HostDistributionStagedOutput? staged = null;
            AsyncDistributionOutput? output = null;
            Task? worker = null;
            await Task.Yield();
            try
            {
                LocalNodeSnapshot snapshot;
                Pid groupLeader;
                SequenceTraceToken? token;
                lock (gate)
                {
                    Validate();
                    snapshot = runtime.LocalIdentity.Snapshot;
                    groupLeader = caller.GroupLeader;
                    resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference) ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
                    ticket = ReserveOwnedOutput(connected, new object());
                    request = new(
                        entry,
                        connected,
                        entry.ConnectionId,
                        caller,
                        runtime.ReserveConnectedSpawnReference(caller, options.Spawn),
                        options
                    )
                    { MonitorCustody641 = input641.Borrow641(caller) };
                    AddConnectedSpawnRequest641(request);
                    token = runtime.SequenceTracing.ConnectedSpawnSent(
                        caller,
                        entry.Node,
                        request.Reference,
                        groupLeader,
                        description,
                        ConnectedSpawnWireOptions.Normalize(description.Options)
                    );
                }
                Term creatorOptions = ConnectedSpawnWireOptions.Normalize(description.Options);
                var control = token is null
                 ? Term.Tuple(
                     Term.I(DistributionControlOpcodes.SpawnRequest),
                     request.Reference,
                     caller.Self,
                     groupLeader,
                     description.Mfa,
                     creatorOptions
                 )
                 : Term.Tuple(
                     Term.I(DistributionControlOpcodes.SpawnRequestTrace),
                     request.Reference,
                     caller.Self,
                     groupLeader,
                     description.Mfa,
                     creatorOptions,
                     token.ToTerm()
                 );
                var packet = new HostDistributionBuffer([DistributionPassThroughCodec.Encode(
                    new(control,description.Arguments),
                    negotiatedFlags:connected.Flags,
                    options:new EtfEncodingOptions{NodeIdentity=runtime.LocalIdentity,NodeSnapshot=snapshot,FunctionOwner=caller.Self}
                ).AsMemory()]);
                lock (gate)
                {
                    Validate();
                    staged = connected.Queue.TryStageOutput(connected.QueueHandle, packet) ?? throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    output = new(
                        entry,
                        connected,
                        request.ConnectionId,
                        ticket,
                        resource,
                        staged
                    );
                    // The existing worker owns the real ticket, staged bytes and resource until
                    // physical queue completion/retirement. Acceptance precedes guest return.
                    worker = PublishAsyncDistributionOutput(output);
                    output.Acceptance.TrySetResult(true);

                    return request.Reference;
                }
            }
            catch (OperationCanceledException) when (request is not null && !CapturedOutputGenerationIsCurrent(entry, connected, request.ConnectionId))
            {
                // Real generation retirement has already delivered the recorded request failure.
                return request.Reference;
            }
            catch (Exception primary)
            {
                if (request is not null) lock (gate) RemoveConnectedSpawnRequest641(request.Reference);
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
                        try
                        {
                            resource?.Dispose();
                        }
                        finally
                        {
                            if (ticket is not null) ReleaseOwnedOutput(connected, ticket);
                        }
                    }
                }
            }

            void Validate()
            {
                caller.Cancellation.ThrowIfCancellationRequested();
                RequireProcess(caller);
                if (!IsConnectedSignalGeneration(entry, connected, request?.ConnectionId ?? entry.ConnectionId)) throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired);
            }

        }
        finally
        {
            input641.Close641();
        }
    }
}
