namespace Erlang.Tests;

internal static class PendingCandidateHostChecks451
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var remote = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await remote.Initialize(ConnectedSpawnHostNames.NodeB);
        await remote.Connect(local);
        var handle = local.Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node));
        using var retained = local.Runtime.DistributionPending.Acquire(handle);
        Require(retained.Snapshot().State == DistributionPendingState.Pending);
        Require(handle.Equals(local.Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node))));
        await using var first = new RuntimeNodeFoundationHostActor(local.Runtime);
        await using var second = new RuntimeNodeFoundationHostActor(local.Runtime);
        var contenders = new[] { first, second };
        var accepted = await Task.WhenAll(contenders.Select(actor => actor.Invoke(context =>
        {
            try
            {
                local.Runtime.DistributionPending.AdoptConnected(
                    handle,
                    context,
                    ConnectedSpawnHostNames.Flags,
                    remote.Kernel.Process.Pid.Creation
                );

                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));
        Require(accepted.Count(value => value) == 1);
        var winner = contenders[Array.IndexOf(accepted, true)];
        var loser = contenders[Array.IndexOf(accepted, false)];
        await loser.DisposeAsync();
        Require(retained.Snapshot().State == DistributionPendingState.Connected);
        Require(local.Runtime.DistributionPending.ConnectedSnapshot(handle).Controller.Equals(winner.Process.Pid));
        Require(!local.Runtime.DistributionPending.AbortPending(handle));
        await using var consumer = new RuntimeNodeFoundationHostActor(remote.Runtime);
        await consumer.Invoke(context =>
        {
            remote.Runtime.Register(ConnectedRegisteredSendHostNames448.Receiver, context.Self);

            return true;
        });
        Term payload = Term.A(PendingCandidateHostNames451.Payload);
        await local.Kernel.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            ConnectedRegisteredSendHostNames448.Send,
            Term.Tuple(Term.A(ConnectedRegisteredSendHostNames448.Receiver), Term.A(remote.Runtime.Node)),
            payload
        ));
        var buffer = await winner.Invoke(context => local.Runtime.DistributionPending.TakeConnectedOutput(handle, context, out _));
        Require(buffer is not null);
        await remote.PutPacket(buffer!.Segments.SelectMany(segment => segment.ToArray()).ToArray());
        Term? received = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
        Require(received!.Equals(payload));
        Require(local.Runtime.DistributionPending.ConnectedSnapshot(handle).QueuedPackets == 0);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(PendingCandidateHostNames451.Failure);
    }
}
