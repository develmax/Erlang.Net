using Erlang;
using System.Diagnostics;

namespace Erlang.Tests;

internal static class DistributionExitLifecycleHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);

            return await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionExitLifecycleHostNames.Local),
                Term.I(0)
            );
        });
        Term host = runtime.DistributionPending.BeginPending(Term.A(DistributionExitLifecycleHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(DistributionExitLifecycleHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(DistributionExitLifecycleHostNames.Creation))
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        var remote = new Pid(
            DistributionExitLifecycleHostNames.Remote,
            DistributionExitLifecycleHostNames.RemoteId,
            DistributionExitLifecycleHostNames.Creation
        );
        await input.Invoke(context =>
        {
            runtime.DistributionPending.ApplyConnectedLinkSignal(
                host,
                context,
                remote,
                target.Process.Pid
            );

            return true;
        });
        await target.InvokeAsync(async context =>
        {
            _ = await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero);
            Term links = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.A(ProcessInfoNames.Links)
            );
            Check(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(remote))));

            return true;
        });
        Term filler = Term.A(DistributionExitLifecycleHostNames.Filler);
        var message = DistributionPassThroughCodec.Send(remote, filler);
        int accepted = 0;
        while (true)
        {
            try
            {
                runtime.DistributionPending.EnqueueConnectedOutput(host, message);
                accepted++;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
        Check(accepted > 0 && runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == accepted);
        Task ending = target.DisposeAsync().AsTask();
        var clock = Stopwatch.StartNew();
        while (runtime.IsAlive(target.Process.Pid))
        {
            if (clock.Elapsed >= DistributionExitLifecycleHostNames.Deadline)
                throw new TimeoutException(DistributionExitLifecycleHostNames.Failure);
            await Task.Yield();
        }
        Check(!target.Process.Completion.IsCompleted && !ending.IsCompleted);
        int seenFiller = 0;
        int seenExit = 0;
        while (seenFiller != accepted || seenExit == 0)
        {
            HostDistributionBuffer? buffer = await controller.Invoke(context => runtime.DistributionPending.TakeConnectedOutput(host, context, out _));
            if (buffer is null)
            {
                if (clock.Elapsed >= DistributionExitLifecycleHostNames.Deadline)
                    throw new TimeoutException(DistributionExitLifecycleHostNames.Failure);
                await Task.Yield();
                continue;
            }
            Check(buffer.SegmentCount == 1);
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(buffer.Segments[0].ToArray());
            if (packet.Payload is not null)
            {
                Check(packet.Payload.Equals(filler));
                seenFiller++;
            }
            else
            {
                Check(packet.Control.Equals(Term.Tuple(
                    Term.I(DistributionControlOpcodes.Exit),
                    target.Process.Pid,
                    remote,
                    Term.A(ProcessExitReasons.Normal)
                )));
                seenExit++;
            }
        }
        await ending.WaitAsync(DistributionExitLifecycleHostNames.Deadline);
        Check((await target.Process.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        Check(seenFiller == accepted && seenExit == 1);
        Check(runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == 0);
        Check(runtime.IsAlive(controller.Process.Pid));
        runtime.DistributionPending.CloseConnected(host);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionExitLifecycleHostNames.Failure);
    }
}
