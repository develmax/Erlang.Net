namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly Dictionary<ReferenceTerm, DistributionSpawnRequest> connectedSpawnRequests = new();

    internal async ValueTask<ReferenceTerm?> TryStartConnectedSpawnRequestAsync(
        ProcessContext owner,
        string node,
        SequenceSpawnDescription description,
        ProcessSpawnRequestOptions options
    )
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
                groupLeader = owner.GroupLeader;
                request = new(
                    entry,
                    connected,
                    entry.ConnectionId,
                    owner,
                    runtime.ReserveConnectedSpawnReference(owner, options.Spawn),
                    options
                );
                connectedSpawnRequests.Add(request.Reference, request);
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
                SequenceTraceToken? token;
                lock (gate)
                {
                    owner.Cancellation.ThrowIfCancellationRequested();
                    RequireProcess(owner);
                    token = runtime.SequenceTracing.ConnectedSpawnSent(
                        owner,
                        request.Entry.Node,
                        request.Reference,
                        groupLeader,
                        description,
                        creatorOptions
                    );
                }
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
                    connectedSpawnRequests.Remove(request.Reference);
                throw;
            }
            finally
            {
                ReleaseOwnedOutput(request.Connection, ticket);
            }
        }
    }

    private void RetireConnectedSpawnRequests(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected)
    {
        foreach (var request in connectedSpawnRequests.Values.Where(value =>
            ReferenceEquals(value.Entry, entry) && ReferenceEquals(value.Connection, connected) && !value.Replied).ToArray())
        {
            connectedSpawnRequests.Remove(request.Reference);
            if (!request.Abandoned && request.OwnerExitReason is null && request.Options.ErrorReply)
                request.Owner.Mailbox.SendTraced(
                    Term.Tuple(
                        request.Options.Tag,
                        request.Reference,
                        Term.A(ProcessSpawnRequestNames.Error),
                        Term.A(NodeSpawnNames.Disconnected)
                    ),
                    null,
                    preserve: true,
                    from: entry.Node
                );
        }
    }

    private void ConnectedSpawnOwnerTerminated(ProcessContext owner, Term reason)
    {
        foreach (var request in connectedSpawnRequests.Values.Where(value => ReferenceEquals(value.Owner, owner)).ToArray())
        {
            if (request.Replied)
                connectedSpawnRequests.Remove(request.Reference);
            else
                request.OwnerExitReason = reason;
        }
    }
}
