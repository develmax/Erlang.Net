namespace Erlang.Tests;

public static class ConnectedSpawnCapacityHostChecks
{
    public static async Task ProducerDeathDuringCapacityWait()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        var filler = DistributionPassThroughCodec.Send(b.Kernel.Process.Pid, Term.A(ConnectedSpawnHostNames.Unrelated));
        int capacity = 0;
        while (true)
        {
            try
            {
                a.Runtime.DistributionPending.EnqueueConnectedOutput(a.Handle!, filler);
                capacity++;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool returned = false;
        var producer = a.Runtime.Spawn(async context =>
        {
            try
            {
                ValueTask<Term> operation = a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.SpawnRequest,
                    Term.A(b.Runtime.Node),
                    Term.A(ConnectedSpawnHostNames.TargetModule),
                    Term.A(ConnectedSpawnHostNames.TargetFunction),
                    Nil.Value,
                    Nil.Value
                );
                if (operation.IsCompleted || capacity == 0)
                    throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
                entered.TrySetResult();
                await operation;
                returned = true;

                return Nil.Value;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        await a.Controller.Invoke(context =>
        {
            a.Runtime.Exit(context, producer.Pid, Term.A(ConnectedSpawnHostNames.Kill));

            return true;
        });
        Term reason = await producer.Completion.WaitAsync(ConnectedSpawnHostNames.Deadline);
        await unwound.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        if (returned || !reason.Equals(Term.A(ConnectedSpawnHostNames.Killed)) ||
            a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets != capacity)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        for (int index = 0; index < capacity; index++)
        {
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(await a.TakePacket(), negotiatedFlags: ConnectedSpawnHostNames.Flags);
            if (!packet.Payload!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)))
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        }
        // A cancelled producer must release its ticket so a later real request can publish and complete.
        await using var next = new RuntimeNodeFoundationHostActor(a.Runtime);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        var reference = (ReferenceTerm)await next.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                Nil.Value
            ));
        await b.PutPacket(await a.TakePacket());
        await a.PutPacket(await b.TakePacket());
        await next.InvokeAsync(async context =>
        {
            var reply = await context.ReceiveAsync(
                value => value is TupleTerm
                {
                    Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var actual, Atom { Name: ConnectedSpawnHostNames.Success }, Pid]
                } tuple && actual.Equals(reference) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            if (reply is null)
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);

            return true;
        });
    }
}
