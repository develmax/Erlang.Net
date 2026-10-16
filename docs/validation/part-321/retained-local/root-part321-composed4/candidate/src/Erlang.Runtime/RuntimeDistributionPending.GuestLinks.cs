namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal bool TryCaptureGuestLinkConnection(
        Pid remote,
        out RuntimeDistributionPendingEntry entry,
        out DistributionConnectedEntry connected
    )
    {
        lock (gate)
        {
            if (entries.TryGetValue(remote.Node, out var weak) && weak.TryGetTarget(out var found) &&
                found.Connected is { } active && IsConnectedSignalGeneration(found, active, found.ConnectionId))
            {
                entry = found;
                connected = active;

                return true;
            }
            entry = null!;
            connected = null!;

            return false;
        }
    }

    internal async ValueTask<Task> PrepareGuestUnlinkOutput(DistributionProcessLink link, ulong unlinkId)
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
                return Task.CompletedTask;
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
            // The guest adapter can hold the outer runtime gate. Encode on a later turn outside that gate.
            if (System.Threading.Monitor.IsEntered(gate))
                await Task.Yield();
            producer.Cancellation.ThrowIfCancellationRequested();
            var marker = new ProcessUnlinkIdentity();
            var packet = new HostDistributionBuffer([DistributionPassThroughCodec.EncodeProcessControl(
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.UnlinkId),
                    new Integer(unlinkId),
                    producer.Self,
                    link.Remote
                ),
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
                if (!IsConnectedSignalGeneration(entry, connected, connectionId) || !runtime.GuestUnlinkStillCurrent(link))
                    return Task.CompletedTask;
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
                // Reuse320's retained connection-only worker; it cannot publish until the local commit resolves acceptance.
                worker = PublishAsyncDistributionOutput(output);
                runtime.CommitGuestUnlink(link, unlinkId, marker);
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
