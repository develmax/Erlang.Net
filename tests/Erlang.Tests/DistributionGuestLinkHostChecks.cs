using Erlang;
using Routes = Erlang.Tests.DistributionGuestLinkPublicRouteHostChecks;
using static Erlang.Tests.DistributionGuestLinkPublicRouteHostChecks;

namespace Erlang.Tests;

internal static class DistributionGuestLinkHostChecks
{
    internal static async Task Run()
    {
        await DuplicatePriorityAndOptions();
        await PublicOrder();
        await LinkExistsBeforeWait();
        await UnlinkSuppressesExitBeforeWait();
        await UnlinkBeforeInboundCallback();
        await RelinkSurvivesOldAcknowledgement();
        await KillBlockedLinkJoinsTerminalOwner();
        await KillBlockedUnlinkRetainsAcceptedOutput();
        await RetirementDoesNotReplay();
        await AcceptedInboundLinkSurvivesRetirementUntilAdmission();
        await RetiredInboundLinkDoesNotReplaceNewConnectionIdentity();
        await UnlinkedPendingAndNewInboundRelationsKeepTheirOwnIdentity();
    }

    private static async Task DuplicatePriorityAndOptions()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Call(fixture, ErlangModuleNames.Link, Term.List(Term.A(DistributionGuestLinkHostNames.Priority)));
        Routes.Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        Routes.Check(Priority(fixture));
        foreach (Term options in new Term[]
        {
            Term.List(Term.A(DistributionGuestLinkHostNames.Priority), Term.A(DistributionGuestLinkHostNames.InvalidOption)),
            new Cons(Term.A(DistributionGuestLinkHostNames.Priority), Term.A(DistributionGuestLinkHostNames.InvalidOption))
        })
        {
            try
            {
                await Call(fixture, ErlangModuleNames.Link, options);
                Routes.Check(false);
            }
            catch (ErlangException failure)
            {
                Routes.Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            }
            Routes.Check(Priority(fixture));
        }
        await Call(fixture, ErlangModuleNames.Link);
        Routes.Check(!Priority(fixture));
        await Call(fixture, ErlangModuleNames.Link, Term.List(Term.A(DistributionGuestLinkHostNames.Priority)));
        Routes.Check(Priority(fixture));
        await Call(fixture, ErlangModuleNames.Link, Nil.Value);
        Routes.Check(!Priority(fixture));
        Empty(fixture);
        Routes.Check(await Receive(fixture) is null);
    }

    private static async Task PublicOrder()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestLinkHostNames.GroupLeader,
                context.Self,
                fixture.Connection.Remote
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Link,
                fixture.Connection.Remote
            );
            Term payload = Term.A(DistributionGuestLinkHostNames.Marker);
            Routes.Check((await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestLinkHostNames.Send,
                fixture.Connection.Remote,
                payload
            )).Equals(payload));
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Unlink,
                fixture.Connection.Remote
            );

            return true;
        });
        Routes.Check((await TakeNow(fixture)).Control.Equals(Term.Tuple(Term.I(DistributionControlOpcodes.GroupLeader),
            fixture.Producer.Process.Pid, fixture.Connection.Remote)));
        Routes.Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        Routes.Check((await TakeNow(fixture)).Payload!.Equals(Term.A(DistributionGuestLinkHostNames.Marker)));
        Routes.Check((await TakeNow(fixture)).Control.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)));
        Empty(fixture);
    }

    private static async Task LinkExistsBeforeWait()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        int capacity = fixture.Fill(fixture.Connection);
        var entered = Signal();
        Task<Term> linking = Start(fixture, ErlangModuleNames.Link, entered);
        await entered.Task.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await WaitForLinkCommit(fixture);
        Routes.Check(!linking.IsCompleted);
        Routes.Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        await fixture.DrainFiller(fixture.Connection, 1);
        Routes.Check((await linking.WaitAsync(DistributionGuestLinkHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)));
        await fixture.DrainFiller(fixture.Connection, capacity - 1);
        Routes.Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        Routes.Check(await Receive(fixture) is null);
    }

    private static async Task UnlinkSuppressesExitBeforeWait()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Call(fixture, ErlangModuleNames.Link);
        await TakeNow(fixture);
        int capacity = fixture.Fill(fixture.Connection);
        var entered = Signal();
        Task<Term> unlinking = Start(fixture, ErlangModuleNames.Unlink, entered);
        await entered.Task.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await WaitForUnlinkCommit(fixture);
        Routes.Check(!unlinking.IsCompleted && (await Links(fixture)).Equals(Nil.Value));
        await Put(fixture, ExitControl(fixture));
        await fixture.DrainFiller(fixture.Connection, 1);
        await unlinking.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await fixture.DrainFiller(fixture.Connection, capacity - 1);
        var unlink = (await TakeNow(fixture)).Control;
        Routes.Check(unlink.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)));
        Routes.Check(await Receive(fixture) is null);
        await Put(fixture, Acknowledgement(fixture, unlink.Items[DistributionGuestLinkHostNames.UnlinkIdIndex]));
        Routes.Check(await Receive(fixture) is null);
        await Call(fixture, ErlangModuleNames.Link);
        Routes.Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        await Put(fixture, ExitControl(fixture));
        Routes.Check((await Receive(fixture))!.Equals(ExitMessage(fixture)));
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
    }

    private static async Task UnlinkBeforeInboundCallback()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
        // Do not receive yet: the public unlink must prevent the queued inbound callback from resurrecting a relation.
        await Call(fixture, ErlangModuleNames.Unlink);
        Routes.Check((await TakeNow(fixture)).Control.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)));
        Routes.Check(await Receive(fixture) is null);
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
    }

    private static async Task RelinkSurvivesOldAcknowledgement()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Call(fixture, ErlangModuleNames.Link);
        await TakeNow(fixture);
        await Call(fixture, ErlangModuleNames.Unlink);
        var oldUnlink = (await TakeNow(fixture)).Control;
        await Call(fixture, ErlangModuleNames.Unlink);
        Empty(fixture);
        await Call(fixture, ErlangModuleNames.Link);
        Routes.Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        await Put(fixture, Acknowledgement(fixture, oldUnlink.Items[DistributionGuestLinkHostNames.UnlinkIdIndex]));
        Routes.Check(await Receive(fixture) is null);
        Routes.Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        await Put(fixture, ExitControl(fixture));
        Routes.Check((await Receive(fixture))!.Equals(ExitMessage(fixture)));
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
    }

    private static async Task KillBlockedLinkJoinsTerminalOwner()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        int capacity = fixture.Fill(fixture.Connection);
        var entered = Signal();
        Task<Term> linking = Start(fixture, ErlangModuleNames.Link, entered);
        await entered.Task.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await WaitForLinkCommit(fixture);
        Routes.Check(!linking.IsCompleted);
        await fixture.Connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(ProcessExitSignals.Kill));

            return true;
        });
        try
        {
            await linking.WaitAsync(DistributionGuestLinkHostNames.Deadline);
            Routes.Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        // Native LINK is already enqueued before busy suspension, so it survives before terminal EXIT.
        await fixture.DrainFiller(fixture.Connection, capacity);
        Routes.Check((await fixture.Take(fixture.Connection)).Control.Equals(LinkControl(fixture)));
        var terminal = await fixture.Take(fixture.Connection);
        Routes.Check(terminal.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            fixture.Producer.Process.Pid,
            fixture.Connection.Remote,
            Term.A(ProcessExitReasons.Killed)
        )));
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        Empty(fixture);
    }

    private static async Task RetirementDoesNotReplay()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        Routes.Check(fixture.Fill(fixture.Connection) > 0);
        var entered = Signal();
        Task<Term> linking = Start(fixture, ErlangModuleNames.Link, entered);
        await entered.Task.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await WaitForLinkCommit(fixture);
        Routes.Check(!linking.IsCompleted);
        fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        Routes.Check((await linking.WaitAsync(DistributionGuestLinkHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)));
        var replacement = await fixture.CreateConnection(fixture.Connection.Remote.Node);
        Routes.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(replacement.Host).QueuedPackets == 0);
        Routes.Check((await Receive(fixture))!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit),
            fixture.Connection.Remote, Term.A(DistributionGuestLinkHostNames.Disconnected))));
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
        Routes.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(replacement.Host).QueuedPackets == 0);
        await fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            replacement.Remote
        ));
        Routes.Check((await fixture.Take(replacement)).Control.Equals(Term.Tuple(Term.I(DistributionControlOpcodes.Link),
            fixture.Producer.Process.Pid, replacement.Remote)));
    }

    private static async Task AcceptedInboundLinkSurvivesRetirementUntilAdmission()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
        // Keep the accepted LINK pending until its connection has queued the ordered retirement EXIT.
        fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        Routes.Check((await Receive(fixture))?.Equals(Term.Tuple(
            Term.A(ProcessMessageTags.LinkedExit),
            fixture.Connection.Remote,
            Term.A(DistributionGuestLinkHostNames.Disconnected)
        )) == true);
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
        Routes.Check(await Receive(fixture) is null);
        Routes.Check(!fixture.Producer.Process.Completion.IsCompleted);
    }

    private static async Task RetiredInboundLinkDoesNotReplaceNewConnectionIdentity()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
        fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        var replacement = await fixture.CreateConnection(fixture.Connection.Remote.Node);
        // Public LINK installs the replacement identity before the old mailbox callbacks are admitted.
        await fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            replacement.Remote
        ));
        Routes.Check((await fixture.Take(replacement)).Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Link), fixture.Producer.Process.Pid, replacement.Remote)));
        Routes.Check((await Links(fixture)).Equals(Term.List(replacement.Remote)));
        Routes.Check(await Receive(fixture) is null);
        Routes.Check((await Links(fixture)).Equals(Term.List(replacement.Remote)));
        byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            replacement.Remote,
            fixture.Producer.Process.Pid,
            Term.A(DistributionGuestLinkHostNames.ExitReason)
        ));
        Routes.Check((await replacement.Controller.Invoke(context => DistributionInputAdapter.PutData(
            context, replacement.Opaque, new BitString(packet)))).Equals(Term.A(ConnectedDistributionHostNames.Success)));
        Routes.Check((await Receive(fixture))?.Equals(Term.Tuple(
            Term.A(ProcessMessageTags.LinkedExit), replacement.Remote, Term.A(DistributionGuestLinkHostNames.ExitReason))) == true);
        Routes.Check((await Links(fixture)).Equals(Nil.Value));
        Routes.Check(await Receive(fixture) is null);
    }

    private static async Task UnlinkedPendingAndNewInboundRelationsKeepTheirOwnIdentity()
    {
        await using (var fixture = await DistributionLifecycleHostFixture.Create())
        {
            await TrapExits(fixture);
            await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
            await Call(fixture, ErlangModuleNames.Unlink);
            var unlink = (await TakeNow(fixture)).Control;
            Routes.Check(unlink.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)));
            // Retire before any owner receive: the old accepted LINK must not undo public unlink suppression.
            fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
            Routes.Check(await Receive(fixture) is null);
            Routes.Check((await Links(fixture)).Equals(Nil.Value));
            Routes.Check(!fixture.Producer.Process.Completion.IsCompleted);
        }

        await using (var fixture = await DistributionLifecycleHostFixture.Create())
        {
            await TrapExits(fixture);
            await Call(fixture, ErlangModuleNames.Link);
            await TakeNow(fixture);
            await Call(fixture, ErlangModuleNames.Unlink);
            var oldUnlink = (await TakeNow(fixture)).Control;
            // A genuine live inbound LINK may supersede the older identity still awaiting its unlink ACK.
            await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
            Routes.Check(await Receive(fixture) is null);
            Routes.Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
            await Put(fixture, Acknowledgement(fixture, oldUnlink.Items[DistributionGuestLinkHostNames.UnlinkIdIndex]));
            Routes.Check(await Receive(fixture) is null);
            Routes.Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
            await Put(fixture, ExitControl(fixture));
            Routes.Check((await Receive(fixture))?.Equals(ExitMessage(fixture)) == true);
            Routes.Check((await Links(fixture)).Equals(Nil.Value));
            Routes.Check(await Receive(fixture) is null);
        }
    }

    private static async Task KillBlockedUnlinkRetainsAcceptedOutput()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await Call(fixture, ErlangModuleNames.Link);
        await TakeNow(fixture);
        int capacity = fixture.Fill(fixture.Connection);
        var entered = Signal();
        Task<Term> unlinking = Start(fixture, ErlangModuleNames.Unlink, entered);
        await entered.Task.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await WaitForUnlinkCommit(fixture);
        Routes.Check(!unlinking.IsCompleted);
        await fixture.Connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(ProcessExitSignals.Kill));

            return true;
        });
        try
        {
            await unlinking.WaitAsync(DistributionGuestLinkHostNames.Deadline);
            Routes.Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        // Inactive relation emits no terminal EXIT, and producer completion cannot await the full output queue.
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestLinkHostNames.Deadline);
        await fixture.DrainFiller(fixture.Connection, capacity);
        var unlink = (await fixture.Take(fixture.Connection)).Control;
        Routes.Check(unlink.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)) &&
            unlink.Items[1] is Integer { Value.Sign: > 0 } && unlink.Items[2].Equals(fixture.Producer.Process.Pid) &&
            unlink.Items[3].Equals(fixture.Connection.Remote));
        await Put(fixture, Acknowledgement(fixture, unlink.Items[DistributionGuestLinkHostNames.UnlinkIdIndex]));
        Empty(fixture);
    }

    private static async Task WaitForLinkCommit(DistributionLifecycleHostFixture fixture)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!(await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)))
        {
            Routes.Check(deadline.Elapsed < DistributionGuestLinkHostNames.Deadline);
            await Task.Yield();
        }
    }

    private static async Task WaitForUnlinkCommit(DistributionLifecycleHostFixture fixture)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!(await Links(fixture)).Equals(Nil.Value))
        {
            Routes.Check(deadline.Elapsed < DistributionGuestLinkHostNames.Deadline);
            await Task.Yield();
        }
    }

    private static Task<Term> Start(DistributionLifecycleHostFixture fixture, string function, TaskCompletionSource entered)
        => fixture.Producer.InvokeAsync(async context =>
        {
            ValueTask<Term> operation = fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                fixture.Connection.Remote
            );
            entered.TrySetResult();

            return await operation;
        });

    private static bool Priority(DistributionLifecycleHostFixture fixture)
    {
        var context = fixture.Producer.Ready.GetAwaiter().GetResult();
        object gate = typeof(ProcessRuntime).GetField(
            DistributionGuestLinkHostNames.RuntimeGateMember, DistributionGuestLinkHostNames.Members)!.GetValue(fixture.Runtime)!;
        lock (gate)
        {
            var links = (HashSet<Pid>)typeof(ProcessContext).GetProperty(
                DistributionGuestLinkHostNames.PriorityMember, DistributionGuestLinkHostNames.Members)!.GetValue(context)!;

            return links.Contains(fixture.Connection.Remote);
        }
    }

    private static TupleTerm ExitControl(DistributionLifecycleHostFixture fixture)
        => Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            fixture.Connection.Remote,
            fixture.Producer.Process.Pid,
            Term.A(DistributionGuestLinkHostNames.ExitReason)
        );

    private static TupleTerm ExitMessage(DistributionLifecycleHostFixture fixture)
        => Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), fixture.Connection.Remote, Term.A(DistributionGuestLinkHostNames.ExitReason));

    private static TupleTerm Acknowledgement(DistributionLifecycleHostFixture fixture, Term unlinkId)
        => Term.Tuple(
            Term.I(DistributionControlOpcodes.UnlinkIdAck),
            unlinkId,
            fixture.Connection.Remote,
            fixture.Producer.Process.Pid
        );

    private static void Empty(DistributionLifecycleHostFixture fixture)
        => Routes.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
