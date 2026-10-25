namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<Task> PrepareGuestLinkOutput(DistributionProcessLink link, bool priority)
    {
        var entry = link.Entry;
        var connected = link.Connection;
        var producer = link.Local;
        uint connectionId = link.ConnectionId;
        LocalNodeSnapshot snapshot;
        DistributionOutputTicket ticket;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        lock (gate)
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(producer);
            if (!IsConnectedSignalGeneration(entry, connected, connectionId))
            {
                runtime.Link(producer, link.Remote, priority);

                return Task.CompletedTask;
            }
            resource = runtime.ReferenceResources.AcquireDistributionEntry(entry.Reference)
                ?? throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.Retired);
            try
            {
                snapshot = runtime.LocalIdentity.Snapshot;
                ticket = ReserveOwnedOutput(connected, new object());
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
            // Fixed control encoding does not pump the owner or invoke callbacks under its outer runtime gate.
            if (System.Threading.Monitor.IsEntered(gate))
                await Task.Yield();
            producer.Cancellation.ThrowIfCancellationRequested();
            var packet = new HostDistributionBuffer([DistributionPassThroughCodec.EncodeProcessControl(
                Term.Tuple(Term.I(DistributionControlOpcodes.Link), producer.Self, link.Remote),
                negotiatedFlags: connected.Flags,
                options: new EtfEncodingOptions
                {
                    NodeIdentity = runtime.LocalIdentity,
                    NodeSnapshot = snapshot,
                    FunctionOwner = producer.Self
                }
            ).AsMemory()]);
            lock (gate)
            {
                producer.Cancellation.ThrowIfCancellationRequested();
                RequireProcess(producer);
                if (!IsConnectedSignalGeneration(entry, connected, connectionId))
                {
                    // Keep this captured attempt disconnected; never replay it on a replacement connection.
                    runtime.Link(producer, link.Remote, priority);

                    return Task.CompletedTask;
                }
                // An inbound LINK accepted during encoding is already the peer's half of this relation.
                if (runtime.UseExistingGuestLink(link, priority))
                    return Task.CompletedTask;
                runtime.ReserveGuestLinkCommit(link);
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
                // The retained worker validates only captured connection lifetime, never the subsequent link/producer state.
                worker = PublishAsyncDistributionOutput(output);
                runtime.CommitGuestLink(link, priority);
                output.Acceptance.TrySetResult(true);

                return worker;
            }
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
    }
}
