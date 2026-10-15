using Erlang;

namespace Erlang.Tests;

internal static class DistributionGroupLeaderHostChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var localLeader = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            _ = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionGroupLeaderHostNames.Local),
                Term.I(0)
            );

            return true;
        });
        var pending = runtime.DistributionPending;
        Term host = pending.BeginPending(Term.A(DistributionGroupLeaderHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context =>
            await DistributionControllerAdapters.SetNode(
                context,
                Term.A(DistributionGroupLeaderHostNames.Remote),
                context.Self,
                Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
            ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        Pid original = await target.Invoke(runtime.GetGroupLeader);
        await Send(localLeader.Process.Pid, target.Process.Pid);
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(original));
        await Drain();
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(localLeader.Process.Pid));
        await localLeader.DisposeAsync();
        await Send(localLeader.Process.Pid, target.Process.Pid);
        await Drain();
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(localLeader.Process.Pid));
        var external = new Pid(
            DistributionGroupLeaderHostNames.OtherNode,
            DistributionGroupLeaderHostNames.LeaderNumber,
            DistributionGroupLeaderHostNames.LeaderCreation
        );
        await Send(external, target.Process.Pid);
        await Send(target.Process.Pid, target.Process.Pid);
        await Drain();
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(target.Process.Pid));
        await Send(external, target.Process.Pid);
        pending.CloseConnected(host);
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(target.Process.Pid));
        await Drain();
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(external));
        Check(!target.Process.Completion.IsCompleted);
        host = pending.BeginPending(Term.A(DistributionGroupLeaderHostNames.Remote));
        opaque = await controller.InvokeAsync(async context =>
            await DistributionControllerAdapters.SetNode(
                context,
                Term.A(DistributionGroupLeaderHostNames.Remote),
                context.Self,
                Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
            ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        await Send(external, localLeader.Process.Pid);
        Check(!controller.Process.Completion.IsCompleted);
        await UnsupportedTarget(new Pid(runtime.Node, target.Process.Pid.Id, 0));
        await UnsupportedTarget(new Pid(DistributionGroupLeaderHostNames.Remote, target.Process.Pid.Id));
        Check((await target.Invoke(runtime.GetGroupLeader)).Equals(external));
        if (checks != DistributionGroupLeaderHostNames.ExpectedAssertions)
            throw new InvalidOperationException(DistributionGroupLeaderHostNames.Failure);

        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { GroupLeaderAssertions = checks, NativeExecuted = false }));

        async Task Send(Pid leader, Pid recipient)
        {
            byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(
                Term.Tuple(Term.I(DistributionControlOpcodes.GroupLeader), leader, recipient));
            Check((await input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(packet))))
                .Equals(Term.A(ConnectedDistributionHostNames.Success)));
        }

        Task<bool> Drain() => target.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero) is null);

            return true;
        });

        async Task UnsupportedTarget(Pid recipient)
        {
            byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(
                Term.Tuple(Term.I(DistributionControlOpcodes.GroupLeader), external, recipient));
            bool unsupported = false;
            try
            {
                _ = await input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(packet)));
            }
            catch (NotSupportedException)
            {
                unsupported = true;
            }
            Check(unsupported);
            Check(!controller.Process.Completion.IsCompleted);
        }

        void Check(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException(DistributionGroupLeaderHostNames.Failure);
            checks++;
        }
    }
}
