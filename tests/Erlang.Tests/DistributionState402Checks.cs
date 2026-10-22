using Erlang;
using System.Numerics;
using static Erlang.Tests.DistributionState402Names;
using static Erlang.Tests.DistributionState402Access;

namespace Erlang.Tests;

internal static class DistributionState402Checks
{
    internal static async Task PendingIdleStaleAndForeign()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        var table = runtime.DistributionPending;
        TupleTerm handle = table.BeginPending(DistributionState402Atoms.Remote);
        using DistributionPendingLease lease = table.Acquire(handle);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            handle
        )).Equals(DistributionState402Atoms.True));
        DistributionState402Access.Check(lease.Snapshot().State == DistributionPendingState.Idle);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            handle
        )).Equals(DistributionState402Atoms.True));
        TupleTerm newer = table.BeginPending(DistributionState402Atoms.Remote);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            handle
        )).Equals(DistributionState402Atoms.True));
        using (DistributionPendingLease current = table.Acquire(newer))
            DistributionState402Access.Check(current.Snapshot().State == DistributionPendingState.Pending);
        await RejectBadArgument(
            other,
            foreign,
            DistributionState402Atoms.Remote,
            newer
        );
        await RejectBadArgument(
            actor,
            runtime,
            DistributionState402Atoms.Other,
            newer
        );
        await RejectBadArgument(
            actor,
            runtime,
            Term.I(InvalidNodeInteger),
            DistributionState402Atoms.BadHandle
        );
        await RejectBadArgument(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            Term.Tuple(Term.I(0), DistributionState402Atoms.BadHandle)
        );
        var alias = Term.Tuple(
            new Integer(((Integer)newer.Items[0]).Value + (BigInteger.One << NativeSmallAliasBits)),
            newer.Items[1]
        );
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            alias
        )).Equals(DistributionState402Atoms.True));
        using (DistributionPendingLease current = table.Acquire(newer))
            DistributionState402Access.Check(current.Snapshot().State == DistributionPendingState.Idle);
    }

    internal static async Task CreationAndConnected()
    {
        await using (var fresh = new ProcessRuntime())
        await using (var actor = new RuntimeNodeFoundationHostActor(fresh))
            DistributionState402Access.Check((await actor.InvokeAsync(async context => await fresh.Modules.Call(context, ModuleName, CreationFunction))).Equals(DistributionState402Atoms.Undefined));
        await using (var configured = new ProcessRuntime(node: ConfiguredNode))
        await using (var actor = new RuntimeNodeFoundationHostActor(configured))
        {
            DistributionState402Access.Check(configured.Node == ProcessNodeDefaults.Local && !configured.IsDistributionAlive);
            DistributionState402Access.Check((await actor.InvokeAsync(async context => await configured.Modules.Call(context, ModuleName, CreationFunction))).Equals(DistributionState402Atoms.Undefined));
        }
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        ProcessRuntime runtime = fixture.Runtime;
        DistributionLifecycleHostConnection connection = fixture.Connection;
        Term result = await fixture.Producer.InvokeAsync(async context => await runtime.Modules.Call(context, ModuleName, CreationFunction));
        DistributionState402Access.Check(result is Integer integer && integer.Value == fixture.Producer.Process.Pid.Creation && integer.Value > 0 && integer.Value <= uint.MaxValue);
        using DistributionPendingLease state = runtime.DistributionPending.Acquire(connection.Host);
        DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Connected);
        Atom node = Term.A(connection.Remote.Node);
        DistributionState402Access.Check((await Abort(
            fixture.Producer,
            runtime,
            node,
            connection.Host
        )).Equals(DistributionState402Atoms.False));
        DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Connected);
        var handle = (TupleTerm)connection.Host;
        var stale = Term.Tuple(new Integer((((Integer)handle.Items[0]).Value + 1) & ConnectionMask), handle.Items[1]);
        DistributionState402Access.Check((await Abort(
            fixture.Producer,
            runtime,
            node,
            stale
        )).Equals(DistributionState402Atoms.True));
        DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Connected);
    }

    internal static async Task RealExitingTransition()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        ProcessRuntime runtime = fixture.Runtime;
        DistributionLifecycleHostConnection connection = fixture.Connection;
        using DistributionPendingLease state = runtime.DistributionPending.Acquire(connection.Host);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> held = connection.Controller.InvokeAsync(async context =>
        {
            Term result = DistributionInputAdapter.PutData(context, connection.Opaque, new BitString(new byte[] { InvalidPacketHeader }));
            entered.TrySetResult();
            await release.Task.WaitAsync(Deadline);

            return result;
        });
        try
        {
            await entered.Task.WaitAsync(Deadline);
            DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Exiting);
            DistributionState402Access.Check((await Abort(
                fixture.Producer,
                runtime,
                Term.A(connection.Remote.Node),
                connection.Host
            )).Equals(DistributionState402Atoms.True));
            DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Exiting);
        }
        finally
        {
            release.TrySetResult();
            DistributionState402Access.Check((await held.WaitAsync(Deadline)).Equals(DistributionState402Atoms.Ok));
        }
        Task<bool> checkpoint = connection.Controller.InvokeAsync(async context =>
        {
            _ = await context.ReceiveAsync<Term>(message => message, Deadline);

            return true;
        });
        await SettleKilled(checkpoint, connection.Controller.Process.Completion);
        DistributionState402Access.Check(state.Snapshot().State == DistributionPendingState.Idle);
    }
}
