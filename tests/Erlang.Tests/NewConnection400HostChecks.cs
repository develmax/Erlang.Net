using Erlang;
using System.Reflection;
using static Erlang.Tests.NewConnection400HostNames;

namespace Erlang.Tests;

internal static class NewConnection400HostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);

        Task<Term> Call(Term node) => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ModuleName,
            Function,
            node
        ));
        Check(runtime.Modules.HasExport(ModuleName, Function, 1));
        Check(runtime.Modules.SourceOwner(ModuleName, Function, 1) is null);
        Check(!runtime.IsDistributionAlive);
        Term first = await Call(NewConnection400HostAtoms.Remote);
        Check(first is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Integer id && id.Value >= 0 && id.Value <= ConnectionMask && pair.Items[1] is ReferenceTerm);
        Check(first.Equals(await Call(NewConnection400HostAtoms.Remote)));
        using DistributionPendingLease lease = runtime.DistributionPending.Acquire(first);
        Check(lease.Snapshot().State == DistributionPendingState.Pending);
        await Reject(Call(Nil.Value), NewConnection400HostAtoms.BadArgument);
        await Reject(Call(runtime.NodeAtom), NewConnection400HostAtoms.BadArgument);
        await Reject(Call(Term.A(InvalidNode)), NewConnection400HostAtoms.BadArgument);

        await kernel.Invoke(context =>
        {
            runtime.Register(Kernel, context.Self);
            typeof(ProcessRuntime).GetMethod(Admission, BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(runtime, [context, NewConnection400HostAtoms.Local, Term.I(1)]);

            return true;
        });
        await controller.Invoke(context => runtime.DistributionPending.AdoptConnected(
            first,
            context,
            DistributionPassThroughFlags.Mandatory,
            1
        ));
        Check(first.Equals(await Call(NewConnection400HostAtoms.Remote)));
        Check(lease.Snapshot().State == DistributionPendingState.Connected);
        var body = LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules)[(ModuleName, Function, 1)];
        ProcessContext caller = await actor.Ready;
        Action<Exception> reject = (Action<Exception>)typeof(RuntimeDistributionPending)
            .GetMethod(CaptureFailure, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(runtime.DistributionPending, [first])!;
        object gate = typeof(ProcessRuntime).GetField(RuntimeGate, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        lock (gate)
        {
            reject(new InvalidDataException(Invariant));
            Check(lease.Snapshot().State == DistributionPendingState.Exiting);
            ValueTask<Term> repeated = body(caller, [NewConnection400HostAtoms.Remote]);
            Check(repeated.IsCompletedSuccessfully && first.Equals(repeated.Result));
        }
        Task<bool> wake = controller.InvokeAsync(async context =>
        {
            _ = await context.ReceiveAsync<Term>(message => message, Deadline);

            return true;
        });
        Check((await controller.Process.Completion.WaitAsync(Deadline)).Equals(NewConnection400HostAtoms.Killed));
        try
        {
            await wake.WaitAsync(Deadline);
        }
        catch (OperationCanceledException) { }
        catch (ErlangException failure) when (failure.Reason.Equals(NewConnection400HostAtoms.Killed)) { }
        Check(lease.Snapshot().State == DistributionPendingState.Idle);
        Term next = await Call(NewConnection400HostAtoms.Remote);
        var oldHandle = (TupleTerm)first;
        var newHandle = (TupleTerm)next;
        Check(newHandle.Items[1].Equals(oldHandle.Items[1]));
        Check(((Integer)newHandle.Items[0]).Value == ((((Integer)oldHandle.Items[0]).Value + 1) & ConnectionMask));

        await using var limited = new ProcessRuntime();
        limited.InitializeDistributionPending(1);
        await using var limitedActor = new RuntimeNodeFoundationHostActor(limited);
        Term limitedHandle = await limitedActor.InvokeAsync(async context => await limited.Modules.Call(
            context,
            ModuleName,
            Function,
            NewConnection400HostAtoms.Remote
        ));
        using DistributionPendingLease limitedLease = limited.DistributionPending.Acquire(limitedHandle);
        await Reject(
            limitedActor.InvokeAsync(async context => await limited.Modules.Call(
                context,
                ModuleName,
                Function,
                Term.A(OtherRemote)
            )),
            NewConnection400HostAtoms.SystemLimit
        );
    }

    private static async Task Reject(Task<Term> operation, Term reason)
    {
        try
        {
            await operation;
            throw new InvalidOperationException(Invariant);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(reason)) { }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Invariant);
    }
}
