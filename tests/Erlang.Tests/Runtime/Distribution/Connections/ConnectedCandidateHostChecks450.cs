namespace Erlang.Tests;

internal static class ConnectedCandidateHostChecks450
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var remote = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await remote.Initialize(ConnectedSpawnHostNames.NodeB);
        await local.Connect(remote);
        await remote.Connect(local);
        await using var consumer = new RuntimeNodeFoundationHostActor(remote.Runtime);
        await consumer.Invoke(context =>
        {
            remote.Runtime.Register(ConnectedRegisteredSendHostNames448.Receiver, context.Self);

            return true;
        });
        Term payload = Term.A(ConnectedCandidateHostNames450.Payload);
        await local.Kernel.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            ConnectedRegisteredSendHostNames448.Send,
            Term.Tuple(Term.A(ConnectedRegisteredSendHostNames448.Receiver), Term.A(remote.Runtime.Node)),
            payload
        ));
        var oldHandle = local.Handle!;
        using var retained = local.Runtime.DistributionPending.Acquire(oldHandle);
        var before = local.Runtime.DistributionPending.ConnectedSnapshot(oldHandle);
        Require(before.QueuedPackets > 0);
        await using (var candidate = new RuntimeNodeFoundationHostActor(local.Runtime))
        {
            Require(local.Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node)).Equals(oldHandle));
            bool refused = false;
            try
            {
                await candidate.Invoke(context => local.Runtime.DistributionPending.AdoptConnected(
                    oldHandle,
                    context,
                    ConnectedSpawnHostNames.Flags,
                    remote.Kernel.Process.Pid.Creation
                ));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            Require(refused && !local.Runtime.DistributionPending.AbortPending(oldHandle));
        }

        var after = local.Runtime.DistributionPending.ConnectedSnapshot(oldHandle);
        Require(before.Equals(after));
        byte[] packet = await local.TakePacket();
        await remote.PutPacket(packet);
        Term? received = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
        Require(received!.Equals(payload));
        var previousController = local.Controller;
        await local.Reconnect(remote);
        retained.Dispose();
        await previousController.DisposeAsync();
        var current = local.Runtime.DistributionPending.ConnectedSnapshot(local.Handle!);
        Require(current.ConnectionId != before.ConnectionId && current.Controller.Equals(local.Controller.Process.Pid));
        Require(!local.Runtime.DistributionPending.AbortPending(local.Handle!));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ConnectedCandidateHostNames450.Failure);
    }
}
