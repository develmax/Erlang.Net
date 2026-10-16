using Erlang;
using Erlang.Differential;
using WaitApi = Erlang.Tests.DistributionControlWaitPublicRouteHostChecks;

namespace Erlang.Tests;

internal static class DistributionControlWaitHostChecks
{
    internal static async Task Run()
    {
        foreach (string function in new[] { DistributionControlWaitHostNames.Send,
            DistributionControlWaitHostNames.GroupLeader, DistributionControlWaitHostNames.Exit })
        {
            await WaitApi.Query(false, function);
            await WaitApi.Kill(false, function);
        }
        await TrappedMessagesRemainForGuestReceive();
        await AcceptedUnlinkKeepsWorkerAfterInboundKill();
    }

    private static async Task TrappedMessagesRemainForGuestReceive()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionControlWaitHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        int capacity = fixture.Fill(fixture.Connection);
        var guest = WaitApi.Start(fixture, DistributionControlWaitHostNames.Receive,
            WaitApi.Arguments(fixture, DistributionControlWaitHostNames.Receive));
        Task<Term>? query = null;
        Exception? primary = null;
        try
        {
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Term first = Term.A(DistributionControlWaitHostNames.First);
            Term last = Term.A(DistributionControlWaitHostNames.Last);
            Term reason = Term.A(DistributionControlWaitHostNames.Reason);
            Term exit = Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), fixture.Connection.Remote, reason);
            await Send(fixture, guest.Guest.Pid, first);
            await WaitApi.PutExit(fixture, guest.Guest.Pid, reason);
            await Send(fixture, guest.Guest.Pid, last);
            query = WaitApi.Information(
                fixture,
                fixture.Producer,
                guest.Guest.Pid,
                ProcessInfoNames.Messages
            );
            WaitApi.Check((await query.WaitAsync(DistributionControlWaitHostNames.Deadline)).Equals(Term.Tuple(
                Term.A(ProcessInfoNames.Messages), Term.List(first, exit, last))));
            WaitApi.Check(!guest.Result.Task.IsCompleted && !guest.Unwound.Task.IsCompleted);
            WaitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
            await fixture.DrainFiller(fixture.Connection, capacity);
            WaitApi.Check((await fixture.Take(fixture.Connection)).Payload?.Equals(Term.A(DistributionControlWaitHostNames.Marker)) == true);
            WaitApi.Check((await guest.Result.Task.WaitAsync(DistributionControlWaitHostNames.Deadline)).Equals(Term.Tuple(first, exit, last)));
            await guest.Guest.Completion.WaitAsync(DistributionControlWaitHostNames.Deadline);
            WaitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        await WaitApi.Finish(
            fixture,
            guest.Guest,
            guest.Unwound.Task,
            null,
            query,
            primary
        );
    }

    private static async Task AcceptedUnlinkKeepsWorkerAfterInboundKill()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionControlWaitHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        var guest = WaitApi.Start(fixture, DistributionControlWaitHostNames.Unlink, fixture.Connection.Remote);
        Task<Term>? query = null;
        Exception? primary = null;
        try
        {
            Term reference = await WaitApi.Monitor(fixture, fixture.Producer, guest.Guest.Pid);
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            WaitApi.Check((await fixture.Take(fixture.Connection)).Control.Equals(Term.Tuple(
                Term.I(DistributionControlOpcodes.Link), guest.Guest.Pid, fixture.Connection.Remote)));
            int capacity = fixture.Fill(fixture.Connection);
            WaitApi.Check(capacity > 0);
            await Send(fixture, guest.Guest.Pid, Term.A(DistributionControlWaitHostNames.Proceed));
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                query = WaitApi.Information(
                    fixture,
                    fixture.Producer,
                    guest.Guest.Pid,
                    ProcessInfoNames.Links
                );
                Term information = await query.WaitAsync(DistributionControlWaitHostNames.Deadline);
                if (information.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Nil.Value)))
                    break;
                WaitApi.Check(information.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(fixture.Connection.Remote))));
                WaitApi.Check(deadline.Elapsed < DistributionControlWaitHostNames.Deadline);
                await Task.Yield();
            }
            WaitApi.Check(!guest.Unwound.Task.IsCompleted);
            await WaitApi.PutExit(fixture, guest.Guest.Pid, Term.A(ProcessExitSignals.Kill));
            WaitApi.Check((await WaitApi.Down(fixture.Producer, reference))?.Equals(Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                reference,
                Term.A(ProcessMonitorKinds.Process),
                guest.Guest.Pid,
                Term.A(ProcessExitReasons.Killed)
            )) == true);
            await guest.Unwound.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            await guest.Guest.Completion.WaitAsync(DistributionControlWaitHostNames.Deadline);
            WaitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
            // The producer has finished, but accepted immutable UNLINK still belongs to its connection worker.
            await fixture.DrainFiller(fixture.Connection, capacity);
            var unlink = (await fixture.Take(fixture.Connection)).Control;
            WaitApi.Check(unlink.Items.Count == 4 && unlink.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)) &&
                unlink.Items[1] is Integer { Value.Sign: > 0 } && unlink.Items[2].Equals(guest.Guest.Pid) &&
                unlink.Items[3].Equals(fixture.Connection.Remote));
            await WaitApi.Put(
                fixture,
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.UnlinkIdAck),
                    unlink.Items[1],
                    fixture.Connection.Remote,
                    guest.Guest.Pid
                )
            );
            WaitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        await WaitApi.Finish(
            fixture,
            guest.Guest,
            guest.Unwound.Task,
            null,
            query,
            primary
        );
    }

    private static Task<Term> Send(DistributionLifecycleHostFixture fixture, Pid target, Term message)
        => fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionControlWaitHostNames.SendFunction,
            target,
            message
        ));
}
