using Erlang;
using Erlang.Differential;
using WaitApi = Erlang.Tests.DistributionControlWaitPublicRouteHostChecks;

namespace Erlang.Tests;

internal static class DistributionRetainedLinkHostChecks
{
    internal static async Task Run()
    {
        await NormalCapacity();
        await NormalTurn();
        await AsyncCapacity();
        await AsyncTurn();
        await HardCapDoesNotInstallRelation();
    }

    // Both methods and the complete test source use pre-325 APIs for genuine historical RED execution.
    internal static Task NormalCapacity() => Retention(false, false);

    internal static Task NormalTurn() => Retention(false, true);

    internal static Task AsyncCapacity() => Retention(true, false);

    internal static Task AsyncTurn() => Retention(true, true);

    private static async Task Retention(bool asynchronous, bool turn)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionRetainedLinkHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        int capacity = fixture.Fill(fixture.Connection);
        Require(capacity > 0);
        Task<Term>? blocker = turn ? await WaitApi.BlockFirstTicket(fixture) : null;
        var observer = fixture.Connection.Controller;
        var guest = WaitApi.Start(
            fixture,
            DistributionRetainedLinkHostNames.Function,
            fixture.Connection.Remote,
            Term.A(asynchronous ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
            observer.Process.Pid
        );
        Exception? primary = null;
        try
        {
            Term reference = await WaitApi.Monitor(fixture, observer, guest.Guest.Pid);
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            // This public owner query also establishes that the normal caller entered its post-accept control wait.
            Term links = await WaitApi.Information(
                fixture,
                observer,
                guest.Guest.Pid,
                ProcessInfoNames.Links
            )
                .WaitAsync(DistributionControlWaitHostNames.Deadline);
            Require(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(fixture.Connection.Remote))));
            Term expectedReturn = Term.Tuple(Term.A(DistributionRetainedLinkHostNames.Returned), guest.Guest.Pid);
            Term? returned = await observer.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                message => message.Equals(expectedReturn) ? message : null,
                asynchronous ? DistributionControlWaitHostNames.Deadline : TimeSpan.Zero
            ));
            Require(asynchronous ? returned?.Equals(expectedReturn) == true : returned is null);
            Require(!guest.Unwound.Task.IsCompleted);
            await WaitApi.PutExit(fixture, guest.Guest.Pid, Term.A(ProcessExitSignals.Kill));
            Term? down = await WaitApi.Down(observer, reference);
            Require(down?.Equals(Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                reference,
                Term.A(ProcessMonitorKinds.Process),
                guest.Guest.Pid,
                Term.A(ProcessExitReasons.Killed)
            )) == true);
            await guest.Unwound.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Require(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
            await fixture.DrainFiller(fixture.Connection, capacity);
            if (blocker is not null)
            {
                Require((await blocker.WaitAsync(DistributionControlWaitHostNames.Deadline)).Equals(Term.A(DistributionControlWaitHostNames.Marker)));
                Require((await fixture.Take(fixture.Connection)).Payload?.Equals(Term.A(DistributionControlWaitHostNames.Marker)) == true);
            }
            Require((await fixture.Take(fixture.Connection)).Control.Equals(Term.Tuple(
                Term.I(DistributionControlOpcodes.Link), guest.Guest.Pid, fixture.Connection.Remote)));
            Require((await fixture.Take(fixture.Connection)).Control.Equals(Term.Tuple(
                Term.I(DistributionControlOpcodes.Exit),
                guest.Guest.Pid,
                fixture.Connection.Remote,
                Term.A(ProcessExitReasons.Killed)
            )));
            await guest.Guest.Completion.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Require(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
            try
            {
                if (!guest.Guest.Completion.IsCompleted)
                    await WaitApi.PutExit(fixture, guest.Guest.Pid, Term.A(ProcessExitSignals.Kill));
                // Finish retires the connection before joining a guest still blocked on the historical output path.
            }
            catch (Exception cleanupFailure)
            {
                primary = new AggregateException(failure, cleanupFailure);
            }
        }
        await WaitApi.Finish(
            fixture,
            guest.Guest,
            guest.Unwound.Task,
            blocker,
            null,
            primary
        );
    }

    private static async Task HardCapDoesNotInstallRelation()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Require(fixture.Fill(fixture.Connection) > 0);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AsyncDistributionHostNames.ProcessFlag,
                Term.A(AsyncDistributionHostNames.AsyncDistribution),
                Term.A(ErlangBooleanAtoms.True)
            );
            for (int index = 0; index < AsyncDistributionHostParameters.MaximumStagedPackets; index++)
                await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    AsyncDistributionHostNames.Send,
                    fixture.Connection.Remote,
                    Term.A(AsyncDistributionHostNames.First)
                );
            try
            {
                await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Link,
                    fixture.Connection.Remote
                );
                Require(false);
            }
            catch (ErlangException failure)
            {
                Require(failure.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
            }
            Term links = await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.A(ProcessInfoNames.Links)
            );
            Require(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Nil.Value)));

            return Nil.Value;
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionRetainedLinkHostNames.Failure);
    }
}
