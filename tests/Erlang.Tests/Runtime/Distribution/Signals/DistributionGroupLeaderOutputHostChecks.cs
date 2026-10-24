using Erlang;

namespace Erlang.Tests;

internal static class DistributionGroupLeaderOutputHostChecks
{
    internal static async Task Run()
    {
        await RegisteredPublicationAndOrder();
        await BoundedAdmission();
        await ProducerTermination();
        await ReplacementGeneration();
        await LocalAndArgumentContracts();
        await DisconnectedAcceptance();
    }

    private static async Task RegisteredPublicationAndOrder()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        Pid first = fixture.Producer.Process.Pid;
        var second = ExternalLeader();
        await fixture.Producer.InvokeAsync(async context =>
        {
            CheckTrue(await Call(context, first, connection.Remote));
            CheckTrue(await Call(context, second, connection.Remote));
            await fixture.Runtime.DistributionPending.EnqueueConnectedOutputAsync(
                connection.Host,
                context,
                DistributionPassThroughCodec.Send(connection.Remote, Term.A(DistributionGroupLeaderOutputHostNames.Following))
            );

            return true;
        });
        CheckControl(await fixture.Take(connection), first, connection.Remote);
        CheckControl(await fixture.Take(connection), second, connection.Remote);
        Check((await fixture.Take(connection)).Payload?.Equals(Term.A(DistributionGroupLeaderOutputHostNames.Following)) == true);
        CheckEmpty(fixture, connection);
    }

    private static async Task BoundedAdmission()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        int capacity = fixture.Fill(connection);
        Check(capacity > 0);
        var entered = Signal();
        Task<Term> changing = BeginBlocked(fixture, connection, entered);
        await entered.Task.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline);
        Check(!changing.IsCompleted);
        await fixture.DrainFiller(connection, 1);
        CheckTrue(await changing.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline));
        await fixture.DrainFiller(connection, capacity - 1);
        CheckControl(await fixture.Take(connection), ExternalLeader(), connection.Remote);
        CheckEmpty(fixture, connection);
    }

    private static async Task ProducerTermination()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        int capacity = fixture.Fill(connection);
        Check(capacity > 0);
        var entered = Signal();
        Task<Term> changing = BeginBlocked(fixture, connection, entered);
        await entered.Task.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline);
        Check(!changing.IsCompleted);
        await connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(DistributionGroupLeaderOutputHostNames.Kill));

            return true;
        });
        try
        {
            await changing.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline);
            Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline);
        await fixture.DrainFiller(connection, capacity);
        CheckEmpty(fixture, connection);
        var successor = fixture.CreateActor();
        CheckTrue(await successor.InvokeAsync(async context => await Call(context, context.Self, connection.Remote)));
        CheckControl(await fixture.Take(connection), successor.Process.Pid, connection.Remote);
        CheckEmpty(fixture, connection);
    }

    private static async Task ReplacementGeneration()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var previous = fixture.Connection;
        Check(fixture.Fill(previous) > 0);
        var entered = Signal();
        Task<Term> changing = BeginBlocked(fixture, previous, entered);
        await entered.Task.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline);
        Check(!changing.IsCompleted);
        fixture.Runtime.DistributionPending.CloseConnected(previous.Host);
        var replacement = await fixture.CreateConnection(previous.Remote.Node);
        CheckTrue(await changing.WaitAsync(DistributionGroupLeaderOutputHostNames.Deadline));
        CheckEmpty(fixture, replacement);
        CheckTrue(await fixture.Producer.InvokeAsync(async context => await Call(context, context.Self, replacement.Remote)));
        CheckControl(await fixture.Take(replacement), fixture.Producer.Process.Pid, replacement.Remote);
        CheckEmpty(fixture, replacement);
    }

    private static async Task LocalAndArgumentContracts()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var target = fixture.CreateActor();
        Pid leader = fixture.Producer.Process.Pid;
        CheckTrue(await fixture.Producer.InvokeAsync(async context => await Call(context, leader, target.Process.Pid)));
        Check((await target.Invoke(fixture.Runtime.GetGroupLeader)).Equals(leader));
        await target.DisposeAsync();
        CheckTrue(await fixture.Producer.InvokeAsync(async context => await Call(context, target.Process.Pid, fixture.Connection.Remote)));
        CheckControl(await fixture.Take(fixture.Connection), target.Process.Pid, fixture.Connection.Remote);
        await BadArgument(fixture, leader, target.Process.Pid);
        await BadArgument(fixture, Term.A(DistributionGroupLeaderOutputHostNames.InvalidPid), fixture.Connection.Remote);
        await BadArgument(fixture, leader, Term.A(DistributionGroupLeaderOutputHostNames.InvalidPid));
        await using var foreignRuntime = new ProcessRuntime();
        await using var foreign = new RuntimeNodeFoundationHostActor(foreignRuntime);
        await foreign.Ready;
        await BadArgument(fixture, foreign.Process.Pid, fixture.Connection.Remote);
        await BadArgument(fixture, leader, foreign.Process.Pid);
        CheckEmpty(fixture, fixture.Connection);
    }

    private static async Task DisconnectedAcceptance()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        fixture.Runtime.DistributionPending.CloseConnected(connection.Host);
        CheckTrue(await fixture.Producer.InvokeAsync(async context => await Call(context, context.Self, connection.Remote)));
        var replacement = await fixture.CreateConnection(connection.Remote.Node);
        CheckEmpty(fixture, replacement);
    }

    private static Task<Term> BeginBlocked(
        DistributionLifecycleHostFixture fixture,
        DistributionLifecycleHostConnection connection,
        TaskCompletionSource entered
    ) => fixture.Producer.InvokeAsync(async context =>
    {
        ValueTask<Term> operation = Call(context, ExternalLeader(), connection.Remote);
        entered.TrySetResult();

        return await operation;
    });

    private static ValueTask<Term> Call(ProcessContext context, Term leader, Term target) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        DistributionGroupLeaderOutputHostNames.GroupLeader,
        leader,
        target
    );

    private static async Task BadArgument(DistributionLifecycleHostFixture fixture, Term leader, Term target)
    {
        try
        {
            await fixture.Producer.InvokeAsync(async context => await Call(context, leader, target));
            Check(false);
        }
        catch (ErlangException exception)
        {
            Check(exception.Reason.Equals(Term.A(DistributionGroupLeaderOutputHostNames.BadArgument)));
        }
    }

    private static Pid ExternalLeader() => new(
        DistributionGroupLeaderOutputHostNames.ThirdNode,
        DistributionGroupLeaderOutputHostNames.ExternalLeaderId,
        DistributionGroupLeaderOutputHostNames.ExternalLeaderCreation
    );

    private static void CheckControl(DistributionDecodedProcessPacket packet, Pid leader, Pid target)
    {
        Check(packet.Payload is null && packet.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.GroupLeader),
            leader,
            target
        )));
    }

    private static void CheckEmpty(DistributionLifecycleHostFixture fixture, DistributionLifecycleHostConnection connection)
    {
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(connection.Host).QueuedPackets == 0);
    }

    private static void CheckTrue(Term value) => Check(value.Equals(Term.A(ErlangBooleanAtoms.True)));

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionGroupLeaderOutputHostNames.Failure);
    }
}
