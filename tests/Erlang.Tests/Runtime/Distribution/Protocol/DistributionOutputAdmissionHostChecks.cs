using Erlang;
using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionOutputAdmissionHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var sender = new RuntimeNodeFoundationHostActor(runtime);
        await using var follower = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);

            return await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionOutputAdmissionHostNames.Local),
                Term.I(0)
            );
        });
        ProcessContext controllerContext = await controller.Ready;
        ProcessContext senderContext = await sender.Ready;
        ProcessContext followerContext = await follower.Ready;
        var remote = new Pid(
            DistributionOutputAdmissionHostNames.Remote,
            DistributionOutputAdmissionHostNames.RemoteId,
            DistributionOutputAdmissionHostNames.Creation
        );
        Term host = await Connect();
        bool connected = true;
        try
        {
            int capacity = Fill();
            Task first = Send(senderContext, DistributionOutputAdmissionHostNames.First);
            Check(!first.IsCompleted);
            object gate = typeof(RuntimeDistributionPending).GetField(
                DistributionOutputAdmissionHostNames.GateField,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(runtime.DistributionPending)!;
            lock (gate)
            {
                // Hold the real runtime gate after freeing capacity, so the already-reserved
                // asynchronous writer cannot commit until the immediate caller is checked.
                Check(Payload(Take()!).Equals(Term.A(DistributionOutputAdmissionHostNames.Filler)));
                Check(runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == capacity - 1);
                bool wouldBlock = false;
                try
                {
                    runtime.DistributionPending.EnqueueConnectedOutput(host, Message(DistributionOutputAdmissionHostNames.Filler));
                }
                catch (DistributionOutputWouldBlockException)
                {
                    wouldBlock = true;
                }
                Check(wouldBlock && !first.IsCompleted);
            }
            await first.WaitAsync(DistributionOutputAdmissionHostNames.Deadline);
            await Drain(capacity, capacity - 1, DistributionOutputAdmissionHostNames.First);

            Check(Fill() == capacity);
            using var cancel = new CancellationTokenSource();
            Task cancelled = Send(senderContext, DistributionOutputAdmissionHostNames.Cancelled, cancel.Token);
            Task following = Send(followerContext, DistributionOutputAdmissionHostNames.Follower);
            Check(!cancelled.IsCompleted && !following.IsCompleted);
            cancel.Cancel();
            await ExpectCancellation(cancelled);
            Check(runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == capacity);
            await Drain(capacity + 1, capacity, DistributionOutputAdmissionHostNames.Follower);
            await following.WaitAsync(DistributionOutputAdmissionHostNames.Deadline);

            long maximum = (long)typeof(ProcessRuntime).Assembly.GetType(
                DistributionOutputAdmissionHostNames.ParametersType,
                true
            )!.GetField(
                DistributionOutputAdmissionHostNames.PacketBudgetField,
                BindingFlags.Static | BindingFlags.NonPublic
            )!.GetRawConstantValue()!;
            bool oversized = false;
            try
            {
                await runtime.DistributionPending.EnqueueConnectedOutputAsync(
                    host,
                    senderContext,
                    DistributionPassThroughCodec.Send(remote, new BitString(new byte[checked((int)maximum + 1)]))
                ).AsTask().WaitAsync(DistributionOutputAdmissionHostNames.Deadline);
            }
            catch (ArgumentException)
            {
                oversized = true;
            }
            Check(oversized && runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == 0);
            Check(runtime.IsAlive(sender.Process.Pid) && runtime.IsAlive(controller.Process.Pid));

            Check(Fill() == capacity);
            Task stale = Send(senderContext, DistributionOutputAdmissionHostNames.Cancelled);
            Check(!stale.IsCompleted);
            connected = false;
            runtime.DistributionPending.CloseConnected(host);
            await ExpectCancellation(stale);
            host = await Connect();
            connected = true;
            Check(runtime.DistributionPending.ConnectedSnapshot(host).QueuedPackets == 0);
            await Send(senderContext, DistributionOutputAdmissionHostNames.Replacement).WaitAsync(DistributionOutputAdmissionHostNames.Deadline);
            Check(Payload(Take()!).Equals(Term.A(DistributionOutputAdmissionHostNames.Replacement)));
            Check(Take() is null);
        }
        finally
        {
            if (connected)
                runtime.DistributionPending.CloseConnected(host);
        }

        async Task<Term> Connect()
        {
            Term handle = runtime.DistributionPending.BeginPending(Term.A(DistributionOutputAdmissionHostNames.Remote));
            await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
                context,
                Term.A(DistributionOutputAdmissionHostNames.Remote),
                context.Self,
                Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(DistributionOutputAdmissionHostNames.Creation))
            ));

            return handle;
        }

        DistributionPassThroughMessage Message(string marker) => DistributionPassThroughCodec.Send(remote, Term.A(marker));

        Task Send(ProcessContext context, string marker, CancellationToken cancellation = default)
            => runtime.DistributionPending.EnqueueConnectedOutputAsync(
                host,
                context,
                Message(marker),
                cancellation
            ).AsTask();

        HostDistributionBuffer? Take() => runtime.DistributionPending.TakeConnectedOutput(host, controllerContext, out _);

        int Fill()
        {
            int count = 0;
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < DistributionOutputAdmissionHostNames.Deadline)
            {
                try
                {
                    runtime.DistributionPending.EnqueueConnectedOutput(host, Message(DistributionOutputAdmissionHostNames.Filler));
                    count++;
                }
                catch (DistributionOutputWouldBlockException)
                {
                    Check(count > 0);

                    return count;
                }
            }

            throw new TimeoutException(DistributionOutputAdmissionHostNames.Failure);
        }

        async Task Drain(int expected, int expectedFiller, string expectedMarker)
        {
            int fillers = 0;
            int markers = 0;
            var deadline = Stopwatch.StartNew();
            while (fillers + markers < expected)
            {
                if (deadline.Elapsed >= DistributionOutputAdmissionHostNames.Deadline)
                    throw new TimeoutException(DistributionOutputAdmissionHostNames.Failure);
                var buffer = Take();
                if (buffer is null)
                {
                    await Task.Yield();
                    continue;
                }
                Term payload = Payload(buffer);
                if (payload.Equals(Term.A(DistributionOutputAdmissionHostNames.Filler)))
                    fillers++;
                else
                {
                    Check(payload.Equals(Term.A(expectedMarker)));
                    markers++;
                }
            }
            Check(fillers == expectedFiller && markers == 1);
            Check(Take() is null);
        }
    }

    private static Term Payload(HostDistributionBuffer buffer)
    {
        Check(buffer.SegmentCount == 1);

        return DistributionPassThroughCodec.DecodeProcessPacket(buffer.Segments[0].ToArray()).Payload
            ?? throw new InvalidOperationException(DistributionOutputAdmissionHostNames.Failure);
    }

    private static async Task ExpectCancellation(Task task)
    {
        bool cancelled = false;
        try
        {
            await task.WaitAsync(DistributionOutputAdmissionHostNames.Deadline);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        Check(cancelled);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionOutputAdmissionHostNames.Failure);
    }
}
