namespace Erlang.Tests;

internal static class ConnectedRemoteEpochHostChecks449
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var previous = new ConnectedSpawnHostEndpoint();
        await using var next = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await Initialize(previous, ConnectedRemoteEpochHostNames449.PreviousCreation);
        await local.Connect(previous);
        await previous.Connect(local);
        await using var consumer = new RuntimeNodeFoundationHostActor(local.Runtime);
        await consumer.Invoke(context =>
        {
            local.Runtime.Register(ConnectedRemoteEpochHostNames449.Receiver, context.Self);

            return true;
        });
        var address = Term.Tuple(Term.A(ConnectedRemoteEpochHostNames449.Receiver), Term.A(local.Runtime.Node));
        var before = await Transfer(previous);
        byte[] oldWire = ExternalTermFormat.Encode(before.Reference);
        var oldHandle = local.Handle!;
        uint oldConnection = local.Runtime.DistributionPending.ConnectedSnapshot(oldHandle).ConnectionId;
        await previous.Runtime.DisposeAsync();
        await Initialize(next, ConnectedRemoteEpochHostNames449.NextCreation);
        await local.Reconnect(next);
        await next.Connect(local);
        var current = local.Runtime.DistributionPending.ConnectedSnapshot(local.Handle!);
        Require(current.RemoteCreation == ConnectedRemoteEpochHostNames449.NextCreation && current.ConnectionId != oldConnection);
        Require(before.Reference.Creation == ConnectedRemoteEpochHostNames449.PreviousCreation && ExternalTermFormat.Encode(before.Reference).AsSpan().SequenceEqual(oldWire));
        bool rejected = false;
        try
        {
            local.Runtime.DistributionPending.ConnectedSnapshot(oldHandle);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected);
        await local.Controller.Invoke(context =>
        {
            var start = local.Runtime.DistributionPending.ConnectedSnapshot(local.Handle!);
            try
            {
                DistributionInputAdapter.PutData(context, oldHandle, new BitString(before.Packet));
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
            }

            var finish = local.Runtime.DistributionPending.ConnectedSnapshot(local.Handle!);
            Require(start.AcceptedInputPackets == finish.AcceptedInputPackets && start.QueuedPackets == finish.QueuedPackets);

            return true;
        });
        Term? staleDelivery = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, TimeSpan.Zero));
        Require(staleDelivery is null);
        var fresh = await Transfer(next);
        Require(fresh.Reference.Creation == ConnectedRemoteEpochHostNames449.NextCreation && !fresh.Reference.Equals(before.Reference));
        Require(ExternalTermFormat.Encode(before.Reference).AsSpan().SequenceEqual(oldWire));

        async Task<(ReferenceTerm Reference, byte[] Packet)> Transfer(ConnectedSpawnHostEndpoint remote)
        {
            Term reference = await remote.Kernel.InvokeAsync(async context => await remote.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedRemoteEpochHostNames449.MakeReference));
            await remote.Kernel.InvokeAsync(async context => await remote.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRegisteredSendHostNames448.Send,
                address,
                reference
            ));
            byte[] packet = await remote.TakePacket();
            await local.PutPacket(packet);
            Term? received = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
            Require(received is ReferenceTerm);

            return ((ReferenceTerm)received!, packet);
        }
    }

    private static async Task Initialize(ConnectedSpawnHostEndpoint endpoint, uint creation)
    {
        if (!endpoint.Runtime.Modules.HasExport(ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.SetNode, 2))
            CoreModules.Register(endpoint.Runtime.Modules);
        await endpoint.Kernel.InvokeAsync(async context =>
        {
            endpoint.Runtime.Register(ConnectedSpawnHostNames.Kernel, context.Self);

            return await endpoint.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SetNode,
                Term.A(ConnectedSpawnHostNames.NodeB),
                Term.I(creation)
            );
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ConnectedRemoteEpochHostNames449.Failure);
    }
}
