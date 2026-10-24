using Erlang;
using System.Diagnostics;

namespace Erlang.Tests;

internal static class DistributionSystemOutputHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var departed = new RuntimeNodeFoundationHostActor(runtime);
        await departed.DisposeAsync();
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);

            return await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionSystemOutputHostNames.Local),
                Term.I(0)
            );
        });
        Term host = runtime.DistributionPending.BeginPending(Term.A(DistributionSystemOutputHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(DistributionSystemOutputHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(DistributionSystemOutputHostNames.Creation))
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, context.Self));
        var remote = new Pid(
            DistributionSystemOutputHostNames.Remote,
            DistributionSystemOutputHostNames.RemoteId,
            DistributionSystemOutputHostNames.Creation
        );
        Term filler = Term.A(DistributionSystemOutputHostNames.Filler);
        var message = DistributionPassThroughCodec.Send(remote, filler);
        byte[] missingLink = DistributionPassThroughCodec.EncodeProcessControl(Term.Tuple(
            Term.I(DistributionControlOpcodes.Link),
            remote,
            departed.Process.Pid
        ));
        byte[] missingUnlink = DistributionPassThroughCodec.EncodeProcessControl(Term.Tuple(
            Term.I(DistributionControlOpcodes.UnlinkId),
            Term.I(DistributionSystemOutputHostNames.UnlinkId),
            remote,
            departed.Process.Pid
        ));
        // Preserve the original no-pressure contract: accepted input has already queued its reply.
        Check((await Put(missingLink)).Equals(Term.A(DistributionSystemOutputHostNames.Accepted)));
        var immediate = await controller.Invoke(context => runtime.DistributionPending.TakeConnectedOutput(host, context, out _));
        Check(immediate is not null && immediate.SegmentCount == 1);
        Check(DistributionPassThroughCodec.DecodeProcessPacket(immediate!.Segments[0].ToArray()).Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            departed.Process.Pid,
            remote,
            Term.A(ErlangErrorReasons.NoProcess)
        )));
        int accepted = Fill();
        Check(accepted > 0);
        // The same process submits input and must subsequently drain the full output queue.
        // Waiting for reply admission inside put_data would deadlock this command before any drain.
        Check((await Put(missingLink).WaitAsync(DistributionSystemOutputHostNames.Deadline)).Equals(Term.A(DistributionSystemOutputHostNames.Accepted)));
        Check((await Put(missingUnlink).WaitAsync(DistributionSystemOutputHostNames.Deadline)).Equals(Term.A(DistributionSystemOutputHostNames.Accepted)));
        var snapshot = runtime.DistributionPending.ConnectedSnapshot(host);
        Check(snapshot.QueuedPackets == accepted && snapshot.AcceptedInputPackets == DistributionSystemOutputHostNames.InitialAcceptedRequestCount &&
            snapshot.RawInputAttempts == DistributionSystemOutputHostNames.InitialAcceptedRequestCount);
        int seenFiller = 0;
        int seenExit = 0;
        int seenAck = 0;
        var deadline = Stopwatch.StartNew();
        while (seenFiller != accepted || seenExit == 0 || seenAck == 0)
        {
            if (deadline.Elapsed >= DistributionSystemOutputHostNames.Deadline)
                throw new TimeoutException(DistributionSystemOutputHostNames.Failure);
            var buffer = await controller.Invoke(context => runtime.DistributionPending.TakeConnectedOutput(host, context, out _));
            if (buffer is null)
            {
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
            else if (packet.Control.Equals(Term.Tuple(
                Term.I(DistributionControlOpcodes.Exit),
                departed.Process.Pid,
                remote,
                Term.A(ErlangErrorReasons.NoProcess)
            )))
                seenExit++;
            else
            {
                Check(packet.Control.Equals(Term.Tuple(
                    Term.I(DistributionControlOpcodes.UnlinkIdAck),
                    Term.I(DistributionSystemOutputHostNames.UnlinkId),
                    departed.Process.Pid,
                    remote
                )));
                seenAck++;
            }
        }
        Check(seenFiller == accepted && seenExit == 1 && seenAck == 1);
        Check(runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == 0);
        Check(runtime.IsAlive(controller.Process.Pid));
        // A second full queue retires while an owned system reply cannot yet be admitted.
        deadline.Restart();
        while (runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets < accepted)
        {
            Fill();
            if (deadline.Elapsed >= DistributionSystemOutputHostNames.Deadline)
                throw new TimeoutException(DistributionSystemOutputHostNames.Failure);
            await Task.Yield();
        }
        Check((await Put(missingLink).WaitAsync(DistributionSystemOutputHostNames.Deadline)).Equals(Term.A(DistributionSystemOutputHostNames.Accepted)));
        runtime.DistributionPending.CloseConnected(host);

        // Runtime disposal must own and drain the reply task even if it had not started before close.

        Task<Term> Put(byte[] packet) => controller.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionInputNames.Function,
            opaque,
            new BitString(packet)
        ));

        int Fill()
        {
            int count = 0;
            while (true)
            {
                try
                {
                    runtime.DistributionPending.EnqueueConnectedOutput(host, message);
                    count++;
                }
                catch (InvalidOperationException)
                {
                    return count;
                }
            }
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionSystemOutputHostNames.Failure);
    }
}
