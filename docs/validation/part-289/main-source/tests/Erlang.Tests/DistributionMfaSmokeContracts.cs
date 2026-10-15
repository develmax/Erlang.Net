namespace Erlang.Tests;

internal static class DistributionMfaSmokeContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == ErlangModuleNames.Module && (function, arity) is
            (RuntimeSetNodeHostNames.SetNode, 2) or
            (RuntimeSetNodeHostNames.SetNode, 3) or
            (DistributionControllerNames.GetOption, 2) or
            (DistributionControllerNames.SetOption, 3) or
            (DistributionControllerNames.RequestDataNotification, 1) or
            (DistributionControllerNames.GetData, 1) or
            (DistributionControllerNames.InputHandler, 2) or
            (DistributionInputNames.Function, DistributionInputNames.Arity);

    internal static async Task Run(string module, string function, int arity)
    {
        Require(Owns(module, function, arity));
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);
            Require((await Call(
                context,
                RuntimeSetNodeHostNames.SetNode,
                Term.A(DistributionMfaSmokeNames.LocalNode),
                Term.I(DistributionMfaSmokeNames.Creation)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require(runtime.Node == DistributionMfaSmokeNames.LocalNode);

            return true;
        });
        if (function == RuntimeSetNodeHostNames.SetNode && arity == 2)
            return;

        Atom remote = Term.A(DistributionMfaSmokeNames.RemoteNode);
        TupleTerm pending = runtime.DistributionPending.BeginPending(remote);
        Term handle = await controller.InvokeAsync(async context => await Call(
            context,
            RuntimeSetNodeHostNames.SetNode,
            remote,
            context.Self,
            Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), Term.I(DistributionMfaSmokeNames.Creation))
        ));
        Require(handle is ReferenceTerm);
        Require(runtime.DistributionPending.ConnectedSnapshot(pending).RemoteCreation == DistributionMfaSmokeNames.Creation);
        if (function == RuntimeSetNodeHostNames.SetNode)
        {
            runtime.DistributionPending.CloseConnected(pending);

            return;
        }

        await controller.InvokeAsync(async context =>
        {
            switch (function)
            {
                case DistributionControllerNames.GetOption:
                    Require((await Call(
                        context,
                        function,
                        handle,
                        Term.A(DistributionControllerNames.GetSize)
                    )).Equals(Term.A(ErlangBooleanAtoms.False)));
                    break;

                case DistributionControllerNames.SetOption:
                    Require((await Call(
                        context,
                        function,
                        handle,
                        Term.A(DistributionControllerNames.GetSize),
                        Term.A(ErlangBooleanAtoms.True)
                    )).Equals(Term.A(ErlangBooleanAtoms.False)));
                    Require((await Call(
                        context,
                        DistributionControllerNames.GetOption,
                        handle,
                        Term.A(DistributionControllerNames.GetSize)
                    )).Equals(Term.A(ErlangBooleanAtoms.True)));
                    break;

                case DistributionControllerNames.InputHandler:
                    Require((await Call(
                        context,
                        function,
                        handle,
                        input.Process.Pid
                    )).Equals(Term.A(DistributionMfaSmokeNames.Success)));
                    Require(runtime.DistributionPending.ConnectedSnapshot(pending).InputHandler!.Equals(input.Process.Pid));
                    break;

                case DistributionControllerNames.RequestDataNotification:
                    Require((await Call(context, function, handle)).Equals(Term.A(DistributionMfaSmokeNames.Success)));
                    runtime.DistributionPending.EnqueueConnectedOutput(pending, Packet(context.Self));
                    Require(await context.ReceiveAsync<Term>(
                        message => message.Equals(Term.A(DistributionMfaSmokeNames.DataNotification)) ? message : null,
                        RuntimeSetNodeHostParameters.Deadline
                    ) is not null);
                    break;

                case DistributionControllerNames.GetData:
                    Require((await Call(context, function, handle)).Equals(Term.A(DistributionControllerNames.NoData)));
                    DistributionPassThroughMessage packet = Packet(context.Self);
                    byte[] expected = DistributionPassThroughCodec.Encode(packet);
                    runtime.DistributionPending.EnqueueConnectedOutput(pending, packet);
                    Term data = await Call(context, function, handle);
                    Require(Cons.Items(data).Cast<BitString>().SelectMany(segment => segment.ToArray()).SequenceEqual(expected));
                    Require((await Call(context, function, handle)).Equals(Term.A(DistributionControllerNames.NoData)));
                    break;

                case DistributionInputNames.Function:
                    {
                        Require((await Call(
                            context,
                            DistributionControllerNames.InputHandler,
                            handle,
                            context.Self
                        )).Equals(Term.A(DistributionMfaSmokeNames.Success)));
                        Term inputPayload = Term.A(DistributionMfaSmokeNames.PayloadMarker);
                        byte[] inputPacket = DistributionPassThroughCodec.Encode(
                            DistributionPassThroughCodec.Send(context.Self, inputPayload));
                        Require((await Call(
                            context,
                            function,
                            handle,
                            new BitString(inputPacket)
                        ))
                            .Equals(Term.A(DistributionMfaSmokeNames.Success)));
                        Term? selected = await context.ReceiveAsync(
                            value => value.Equals(inputPayload) ? value : null,
                            RuntimeSetNodeHostParameters.Deadline);
                        Require(selected is not null && selected.Equals(inputPayload));
                        Require(runtime.DistributionPending.ConnectedSnapshot(pending).AcceptedInputPackets == 1);
                        break;
                    }

                default:
                    throw new InvalidOperationException(DistributionMfaSmokeNames.Failure);
            }

            return true;
        });
        runtime.DistributionPending.CloseConnected(pending);
    }

    private static DistributionPassThroughMessage Packet(Pid sender)
        => DistributionPassThroughCodec.SendRegistered(sender, Term.A(DistributionMfaSmokeNames.RegisteredTarget), Term.A(DistributionMfaSmokeNames.PayloadMarker));

    private static ValueTask<Term> Call(ProcessContext context, string name, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            name,
            arguments
        );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DistributionMfaSmokeNames.Failure);
    }
}
