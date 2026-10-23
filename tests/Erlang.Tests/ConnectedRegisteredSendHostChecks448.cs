using Erlang.Compiler;

namespace Erlang.Tests;

internal static class ConnectedRegisteredSendHostChecks448
{
    internal static async Task Run()
    {
        await using var sender = new ConnectedSpawnHostEndpoint();
        await using var receiver = new ConnectedSpawnHostEndpoint();
        await sender.Initialize(ConnectedSpawnHostNames.NodeA);
        await receiver.Initialize(ConnectedSpawnHostNames.NodeB);
        await sender.Connect(receiver);
        await receiver.Connect(sender);
        await using var producer = new RuntimeNodeFoundationHostActor(sender.Runtime);
        await using var consumer = new RuntimeNodeFoundationHostActor(receiver.Runtime);
        new Parser(ConnectedRegisteredSendHostNames448.Source).ParseModule().Register(sender.Runtime.Modules);
        await consumer.Invoke(context =>
        {
            receiver.Runtime.Register(ConnectedRegisteredSendHostNames448.Receiver, context.Self);

            return true;
        });
        Term address = Term.Tuple(
            Term.A(ConnectedRegisteredSendHostNames448.Receiver),
            Term.A(receiver.Runtime.Node)
        );
        int sequence = 0;
        foreach (bool asynchronous in new[] { false, true })
        {
            await producer.InvokeAsync(async context => await sender.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRegisteredSendHostNames448.ProcessFlag,
                Term.A(ConnectedRegisteredSendHostNames448.Async),
                CoreModules.Boolean(asynchronous)
            ));
            foreach (int route in Enumerable.Range(0, 5))
            {
                Term payload = Term.Tuple(Term.I(sequence++), CoreModules.Boolean(asynchronous));
                Term result = await producer.InvokeAsync(async context => route switch
                {
                    0 => await sender.Runtime.Modules.Call(
                        context,
                        ConnectedRegisteredSendHostNames448.Module,
                        ConnectedRegisteredSendHostNames448.Bang,
                        address,
                        payload
                    ),
                    1 => await sender.Runtime.Modules.Call(
                        context,
                        ConnectedSpawnHostNames.Erlang,
                        ConnectedRegisteredSendHostNames448.Send,
                        address,
                        payload
                    ),
                    2 => await sender.Runtime.Modules.Call(
                        context,
                        ConnectedSpawnHostNames.Erlang,
                        ConnectedRegisteredSendHostNames448.Send,
                        address,
                        payload,
                        Term.List(Term.A(ConnectedRegisteredSendHostNames448.NoConnect))
                    ),
                    3 => await sender.Runtime.Modules.Call(
                        context,
                        ConnectedSpawnHostNames.Erlang,
                        ConnectedRegisteredSendHostNames448.NoSuspend,
                        address,
                        payload
                    ),
                    _ => await sender.Runtime.Modules.Call(
                        context,
                        ConnectedSpawnHostNames.Erlang,
                        ConnectedRegisteredSendHostNames448.NoSuspend,
                        address,
                        payload,
                        Term.List(Term.A(ConnectedRegisteredSendHostNames448.NoConnect))
                    )
                });
                Require(result.Equals(route < 2 ? payload : route == 2 ? Term.A(ConnectedSpawnHostNames.Success) : CoreModules.Boolean(true)));
                byte[] packet = await sender.TakePacket();
                var decoded = DistributionPassThroughCodec.DecodeProcessPacket(packet, negotiatedFlags: ConnectedSpawnHostNames.Flags);
                Require(decoded.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.RegisteredSend)));
                Require(decoded.Control.Items[DistributionPassThroughLayout.RegisteredTargetIndex].Equals(Term.A(ConnectedRegisteredSendHostNames448.Receiver)) && decoded.Payload!.Equals(payload));
                await receiver.PutPacket(packet);
                Term? delivered = await consumer.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
                Require(delivered!.Equals(payload));
            }
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ConnectedRegisteredSendHostNames448.Failure);
    }
}
