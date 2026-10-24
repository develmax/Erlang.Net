namespace Erlang.Tests;

internal static class RetiredCandidateHostChecks452
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var remote = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await remote.Initialize(ConnectedSpawnHostNames.NodeB);
        var oldHandle = local.Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node));
        using var retained = local.Runtime.DistributionPending.Acquire(oldHandle);
        await using var oldController = new RuntimeNodeFoundationHostActor(local.Runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(local.Runtime);
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queued = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> blocked = oldController.InvokeAsync(async context =>
        {
            entered.TrySetResult(true);
            await release.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);

            return true;
        });
        try
        {
            await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
            Task<bool> obsolete = requester.InvokeAsync(async context =>
            {
                try
                {
                    var adoption = local.Runtime.DistributionPending.AdoptNodeControllerAsync(
                        context,
                        Term.A(remote.Runtime.Node),
                        oldController.Process.Pid,
                        Term.Tuple(new Integer(ConnectedSpawnHostNames.Flags), Term.I(remote.Kernel.Process.Pid.Creation))
                    );
                    Require(!adoption.IsCompleted);
                    queued.TrySetResult(true);
                    await adoption;

                    return false;
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    return true;
                }
            });
            await queued.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
            Require(!obsolete.IsCompleted);
            Require(local.Runtime.DistributionPending.AbortPending(oldHandle));
            var current = local.Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node));
            Require(!current.Equals(oldHandle));
            using var freshLease = local.Runtime.DistributionPending.Acquire(current);
            var pending = freshLease.Snapshot();
            Require((await NativeAbort(local, remote.Runtime.Node, oldHandle)).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(pending.Equals(freshLease.Snapshot()));
            await local.Connect(remote);
            await remote.Connect(local);
            Require(current.Equals(local.Handle));
            var before = local.Runtime.DistributionPending.ConnectedSnapshot(current);
            release.TrySetResult(true);
            await blocked;
            await oldController.InvokeAsync(async context => await context.ReceiveAsync<Term>(value => value, TimeSpan.Zero));
            Require(await obsolete);
            Require(before.Equals(local.Runtime.DistributionPending.ConnectedSnapshot(current)));
            Require((await NativeAbort(local, remote.Runtime.Node, oldHandle)).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await NativeAbort(local, remote.Runtime.Node, current)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Require(before.Equals(local.Runtime.DistributionPending.ConnectedSnapshot(current)));
            await using var consumer = new RuntimeNodeFoundationHostActor(remote.Runtime);
            await consumer.Invoke(context =>
            {
                remote.Runtime.Register(ConnectedRegisteredSendHostNames448.Receiver, context.Self);

                return true;
            });
            Term payload = Term.A(RetiredCandidateHostNames452.Payload);
            await local.Kernel.InvokeAsync(async context => await local.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRegisteredSendHostNames448.Send,
                Term.Tuple(Term.A(ConnectedRegisteredSendHostNames448.Receiver), Term.A(remote.Runtime.Node)),
                payload
            ));
            await remote.PutPacket(await local.TakePacket());
            Term? received = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
            Require(received!.Equals(payload));
            Require(local.Runtime.DistributionPending.ConnectedSnapshot(current).QueuedPackets == 0);
        }
        finally
        {
            release.TrySetResult(true);
            await blocked;
        }
    }

    private static Task<Term> NativeAbort(ConnectedSpawnHostEndpoint local, string node, Term handle)
        => local.Kernel.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            DistributionState402Names.ModuleName,
            DistributionState402Names.AbortFunction,
            Term.A(node),
            handle
        ));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RetiredCandidateHostNames452.Failure);
    }
}
