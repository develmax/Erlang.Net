namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly Dictionary<ReferenceTerm, DistributionSpawnRequest> connectedSpawnRequests = new();

    internal async ValueTask<ReferenceTerm?> TryStartConnectedSpawnRequestAsync(
        ProcessContext owner,
        string node,
        SequenceSpawnDescription description,
        ProcessSpawnRequestOptions options,
        RuntimeProcessPayload641? prepared641 = null
    )
    {
        var input641 = prepared641?.Borrow641(owner)
            ?? RuntimeProcessPayload641.Prepare641(runtime, owner, Term.Tuple(options.Tag, options.Spawn.MonitorOptions.Tag));
        try
        {
            DistributionSpawnRequest request;
            DistributionOutputTicket ticket;
            ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
            LocalNodeSnapshot snapshot;
            Pid groupLeader;
            lock (gate)
            {
                owner.Cancellation.ThrowIfCancellationRequested();
                RequireProcess(owner);
                if (!entries.TryGetValue(node, out var weak) || !weak.TryGetTarget(out var entry) ||
                    entry.Connected is not { } connected || !IsConnectedSignalGeneration(entry, connected, entry.ConnectionId))
                    return null;
                if ((connected.Flags & DistributionPassThroughFlags.Spawn) == 0)
                    throw new ErlangException(DistributionSpawnNames.NotSupported);
                if (owner.AsyncDistribution)
                    throw new ErlangException(DistributionSpawnNames.NotSupported);
                resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                    ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
                try
                {
                    ticket = ReserveOwnedOutput(connected, new object());
                    snapshot = runtime.LocalIdentity.Snapshot;
                    groupLeader = owner.SnapshotGroupLeader();
                    request = new(
                        entry,
                        connected,
                        entry.ConnectionId,
                        owner,
                        runtime.ReserveConnectedSpawnReference(owner, options.Spawn),
                        options
                    )
                    { MonitorCustody641 = input641.Borrow641(owner) };
                    AddConnectedSpawnRequest641(request);
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
                    Term creatorOptions = ConnectedSpawnWireOptions.Normalize(description.Options);
                    if (System.Threading.Monitor.IsEntered(gate))
                        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                    PreparedConnectedSequenceSend sequence;
                    SequenceTraceToken? token;
                    while (true)
                    {
                        sequence = await PreparedConnectedSequenceSend.PrepareAsync(
                            owner,
                            request.Reference,
                            groupLeader,
                            description,
                            creatorOptions
                        ).ConfigureAwait(false);
                        bool accepted;
                        try
                        {
                            lock (gate)
                            {
                                owner.Cancellation.ThrowIfCancellationRequested();
                                RequireProcess(owner);
                                accepted = sequence.IsCurrent(owner);
                                if (accepted)
                                    token = runtime.SequenceTracing.ConnectedSpawnSentPrepared(
                                        owner,
                                        request.Entry.Node,
                                        request.Reference,
                                        groupLeader,
                                        description,
                                        creatorOptions,
                                        sequence
                                    );
                                else token = null;
                            }
                        }
                        catch (Exception primary)
                        {
                            try
                            {
                                await sequence.DisposeAsync().ConfigureAwait(false);
                            }
                            catch (Exception cleanup)
                            {
                                throw new AggregateException(primary, cleanup);
                            }
                            throw;
                        }
                        if (accepted) break;
                        await sequence.DisposeAsync().ConfigureAwait(false);
                    }
                    await using (sequence.ConfigureAwait(false))
                    {
                        var control = token is null
                            ? Term.Tuple(
                                Term.I(DistributionControlOpcodes.SpawnRequest),
                                request.Reference,
                                owner.Self,
                                groupLeader,
                                description.Mfa,
                                creatorOptions
                            )
                            : Term.Tuple(
                                Term.I(DistributionControlOpcodes.SpawnRequestTrace),
                                request.Reference,
                                owner.Self,
                                groupLeader,
                                description.Mfa,
                                creatorOptions,
                                token.ToTerm()
                            );
                        await PublishOwnedPacket(
                            request.Entry,
                            request.Connection,
                            request.ConnectionId,
                            ticket,
                            () =>
         {
             owner.Cancellation.ThrowIfCancellationRequested();
             RequireProcess(owner);
         },
                            () => new HostDistributionBuffer([DistributionPassThroughCodec.Encode(
                                new(control, description.Arguments),
                                negotiatedFlags: request.Connection.Flags,
                                options: new EtfEncodingOptions
                        {
                            NodeIdentity = runtime.LocalIdentity,
                            NodeSnapshot = snapshot,
                            FunctionOwner = owner.Self
                        }
                            ).AsMemory()]),
                            owner.Cancellation,
                            waitingProducer: owner
                        ).ConfigureAwait(false);
                    }

                    return request.Reference;
                }
                catch (OperationCanceledException) when (!CapturedOutputGenerationIsCurrent(request.Entry, request.Connection, request.ConnectionId))
                {
                    // Exact-generation retirement already queued the request failure; never reconnect/replay here.
                    return request.Reference;
                }
                catch
                {
                    lock (gate)
                        RemoveConnectedSpawnRequest641(request.Reference);
                    throw;
                }
                finally
                {
                    ReleaseOwnedOutput(request.Connection, ticket);
                }
            }

        }
        finally
        {
            input641.Close641();
        }
    }

    private void RetireConnectedSpawnRequests(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected)
    {
        foreach (var request in connectedSpawnRequests.Values.Where(value =>
            ReferenceEquals(value.Entry, entry) && ReferenceEquals(value.Connection, connected) && !value.Replied).ToArray())
        {
            var dispatch641 = request.Abandoned || request.OwnerExitReason is not null
                ? null : request.MonitorCustody641?.Borrow641(null);
            try
            {
                RemoveConnectedSpawnRequest641(request.Reference);
                if (!request.Abandoned && request.OwnerExitReason is null && request.Options.ErrorReply)
                    runtime.SendMonitorPayload641(
                        request.Owner,
                        Term.Tuple(
                            dispatch641 is null ? request.Options.Tag : ((TupleTerm)dispatch641.Value641).Items[0],
                            request.Reference,
                            Term.A(ProcessSpawnRequestNames.Error),
                            Term.A(NodeSpawnNames.Disconnected)
                        ),
                        null,
                        dispatch641,
                        preserve: true,
                        from: entry.Node
                    );
            }
            finally
            {
                dispatch641?.Close641();
            }
        }
    }

    private void ConnectedSpawnOwnerTerminated(ProcessContext owner, Term reason, RuntimeProcessPayload641? reason641)
    {
        foreach (var request in connectedSpawnRequests.Values.Where(value => ReferenceEquals(value.Owner, owner)).ToArray())
        {
            request.MonitorCustody641?.Close641();
            if (request.Replied)
                RemoveConnectedSpawnRequest641(request.Reference);
            else
            {
                var next641 = reason641?.Borrow641(null);
                var previous641 = request.OwnerExitCustody641;
                request.OwnerExitCustody641 = next641;
                request.OwnerExitReason = reason;
                previous641?.Close641();
            }
        }
    }
}
