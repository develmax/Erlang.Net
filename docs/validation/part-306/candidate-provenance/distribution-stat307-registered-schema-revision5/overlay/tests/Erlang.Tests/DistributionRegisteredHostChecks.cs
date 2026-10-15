namespace Erlang.Tests;

internal static class DistributionRegisteredHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        Require(runtime.Modules.HasExport(ErlangModuleNames.Module, RuntimeSetNodeHostNames.SetNode, 2));
        foreach (var (name, arity) in DistributionRegisteredHostNames.SixMfas)
            Require(runtime.Modules.HasExport(ErlangModuleNames.Module, name, arity));
        await requester.InvokeAsync(async context =>
        {
            await Reject(() => Call(context, DistributionControllerNames.GetData, Nil.Value), DistributionControllerNames.NotSupported);
            await Reject(() => Call(
                context,
                RuntimeSetNodeHostNames.SetNode,
                Nil.Value,
                Nil.Value,
                Nil.Value
            ), ErlangErrorReasons.BadArgument);

            return true;
        });
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);
            Require((await Call(
                context,
                RuntimeSetNodeHostNames.SetNode,
                Term.A(DistributionRegisteredHostNames.Local),
                Term.I(RuntimeSetNodeHostParameters.Requested)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        });
        Atom remote = Term.A(DistributionRegisteredHostNames.Remote);
        TupleTerm pending = runtime.DistributionPending.BeginPending(remote);
        await requester.InvokeAsync(async context =>
        {
            await Reject(
                () => Call(
                    context,
                    RuntimeSetNodeHostNames.SetNode,
                    remote,
                    controller.Process.Pid,
                    Options(new Integer(System.Numerics.BigInteger.One << RuntimeNodeCreationHostParameters.FirstNonSmallShift))
                ),
                ErlangErrorReasons.BadArgument
            );

            return true;
        });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> adoption = requester.InvokeAsync(async context =>
        {
            ValueTask<Term> result = Call(
                context,
                RuntimeSetNodeHostNames.SetNode,
                remote,
                controller.Process.Pid,
                Options(Term.I((1L << RuntimeNodeCreationHostParameters.WireCreationShift) + DistributionRegisteredHostNames.FirstRemoteCreation))
            );
            started.TrySetResult();

            return await result;
        });
        await started.Task.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        await controller.InvokeAsync(async context =>
        {
            Require(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);

            return true;
        });
        Term opaque = await adoption.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        Require(opaque is TupleTerm && opaque.Equals(pending));
        Require(runtime.DistributionPending.ConnectedSnapshot(pending).RemoteCreation == DistributionRegisteredHostNames.FirstRemoteCreation);
        await kernel.InvokeAsync(async context =>
        {
            Term notice = Term.Tuple(
                Term.A(DistributionRegisteredHostNames.KernelNotice),
                controller.Process.Pid,
                remote,
                requester.Process.Pid
            );
            Require(await context.ReceiveAsync<Term>(message => message.Equals(notice) ? message : null, RuntimeSetNodeHostParameters.Deadline) is not null);
            await Reject(() => Call(context, DistributionControllerNames.GetData, opaque), ErlangErrorReasons.BadArgument);

            return true;
        });
        await controller.InvokeAsync(async context =>
        {
            Require((await Call(context, DistributionControllerNames.GetData, opaque)).Equals(Term.A(DistributionControllerNames.NoData)));
            Require((await Call(
                context,
                DistributionControllerNames.GetOption,
                opaque,
                Term.A(DistributionControllerNames.GetSize)
            )).Equals(Term.A(ErlangBooleanAtoms.False)));
            Require((await Call(
                context,
                DistributionControllerNames.SetOption,
                opaque,
                Term.A(DistributionControllerNames.GetSize),
                Term.A(ErlangBooleanAtoms.True)
            )).Equals(Term.A(ErlangBooleanAtoms.False)));
            Require((await Call(
                context,
                DistributionControllerNames.GetOption,
                opaque,
                Term.A(DistributionControllerNames.GetSize)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            await Reject(
                () => Call(
                    context,
                    DistributionControllerNames.SetOption,
                    opaque,
                    Term.A(DistributionControllerNames.GetSize),
                    Nil.Value
                ),
                ErlangErrorReasons.BadArgument
            );
            Require((await Call(
                context,
                DistributionControllerNames.GetOption,
                opaque,
                Term.A(DistributionControllerNames.GetSize)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await Call(
                context,
                DistributionControllerNames.InputHandler,
                opaque,
                input.Process.Pid
            )).Equals(Term.A(DistributionRegisteredHostNames.Success)));
            Require(runtime.DistributionPending.ConnectedSnapshot(pending).InputHandler!.Equals(input.Process.Pid));
            Require((await Call(context, DistributionControllerNames.RequestDataNotification, opaque)).Equals(Term.A(DistributionRegisteredHostNames.Success)));
            Require((await Call(context, DistributionControllerNames.GetData, pending)).Equals(Term.A(DistributionControllerNames.NoData)));

            return true;
        });
        Term payload = Term.Tuple(Term.A(DistributionRegisteredHostNames.PayloadMarker), Nil.Value);
        DistributionPassThroughMessage packet = DistributionPassThroughCodec.SendRegistered(requester.Process.Pid, Term.A(DistributionRegisteredHostNames.Sink), payload);
        byte[] expected = DistributionPassThroughCodec.Encode(packet);
        runtime.DistributionPending.EnqueueConnectedOutput(pending, packet);
        await controller.InvokeAsync(async context =>
        {
            Require(await context.ReceiveAsync<Term>(
                message => message.Equals(Term.A(DistributionRegisteredHostNames.DataNotice)) ? message : null,
                RuntimeSetNodeHostParameters.Deadline
            ) is not null);
            Term output = await Call(context, DistributionControllerNames.GetData, opaque);
            Require(output is TupleTerm { Items.Count: 2 } sized && sized.Items[0].Equals(Term.I(expected.Length)) && Flatten(sized.Items[1]).SequenceEqual(expected));
            Require((await Call(context, DistributionControllerNames.GetData, opaque)).Equals(Term.A(DistributionControllerNames.NoData)));

            return true;
        });
        runtime.DistributionPending.CloseConnected(pending);
        TupleTerm reopened = runtime.DistributionPending.BeginPending(remote);
        Term current = await controller.InvokeAsync(async context => await Call(
            context,
            RuntimeSetNodeHostNames.SetNode,
            remote,
            context.Self,
            Options(Term.I(RuntimeNodeCreationHostParameters.MaximumSmallCreation))
        ));
        Require(current is TupleTerm && current.Equals(reopened) && !current.Equals(opaque));
        Require(runtime.DistributionPending.ConnectedSnapshot(reopened).RemoteCreation == uint.MaxValue);
        await controller.InvokeAsync(async context =>
        {
            await Reject(() => Call(context, DistributionControllerNames.GetData, opaque), ErlangErrorReasons.BadArgument);
            Require((await Call(context, DistributionControllerNames.GetData, current)).Equals(Term.A(DistributionControllerNames.NoData)));
            Require((await Call(
                context,
                DistributionControllerNames.SetOption,
                current,
                Term.A(DistributionControllerNames.GetSize),
                Term.A(ErlangBooleanAtoms.False)
            )).Equals(Term.A(ErlangBooleanAtoms.False)));

            return true;
        });
        runtime.DistributionPending.CloseConnected(reopened);
    }

    private static Term Options(Term creation) => Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), creation);

    private static byte[] Flatten(Term segments) => Cons.Items(segments).Cast<BitString>().SelectMany(segment => segment.ToArray()).ToArray();

    private static ValueTask<Term> Call(ProcessContext context, string name, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        name,
        arguments
    );

    private static async Task Reject(Func<ValueTask<Term>> action, string reason)
    {
        try
        {
            await action();
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(reason)))
        {
            return;
        }

        throw new InvalidOperationException(DistributionRegisteredHostNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DistributionRegisteredHostNames.Failure);
    }
}
