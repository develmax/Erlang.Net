using Erlang;
using Erlang.Differential;
using WaitApi = Erlang.Tests.DistributionControlWaitPublicRouteHostChecks;

namespace Erlang.Tests;

internal static class ProcessInfoAsyncDistributionHostChecks
{
    internal static async Task Run()
    {
        await DefaultQuery();
        await DirectOtherProcessFlag();
        await MixedOwnerQueryDuringDistributionWait();
    }

    internal static async Task DefaultQuery()
    {
        CompiledModuleCase contract = ProcessInfoAsyncDistributionCases.Modules[0];
        using var compiled = GeneratedModuleCompiler.Compile(contract.Source);
        CompiledModuleStreamResult actual = await CompiledModuleExecution.RunCaptured(compiled);
        Require(actual.Result.Equals(contract.Expected));
        Require(actual.StandardOutputHex.Length == 0 && actual.StandardErrorHex.Length == 0);
    }

    private static async Task DirectOtherProcessFlag()
    {
        await using var runtime = new ProcessRuntime();
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        Term flag = Term.A(ProcessInfoAsyncDistributionCaseNames.Flag);
        Require((await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoAsyncDistributionHostNames.ProcessFlag,
            flag,
            Term.A(ErlangBooleanAtoms.True)
        ))).Equals(Term.A(ErlangBooleanAtoms.False)));
        // This target is deliberately idle on the HOST channel. A pure flag query needs only the shared runtime gate.
        Require((await observer.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target.Process.Pid,
            flag
        ))).Equals(Term.Tuple(flag, Term.A(ErlangBooleanAtoms.True))));
        Require((await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoAsyncDistributionHostNames.ProcessFlag,
            flag,
            Term.A(ErlangBooleanAtoms.False)
        ))).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require((await observer.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target.Process.Pid,
            flag
        ))).Equals(Term.Tuple(flag, Term.A(ErlangBooleanAtoms.False))));
    }

    private static async Task MixedOwnerQueryDuringDistributionWait()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionControlWaitHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        int capacity = fixture.Fill(fixture.Connection);
        var guest = WaitApi.Start(fixture, DistributionControlWaitHostNames.Link, fixture.Connection.Remote);
        Task<Term>? query = null;
        Exception? primary = null;
        try
        {
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Term links = Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(fixture.Connection.Remote));
            Term flag = Term.A(ProcessInfoAsyncDistributionCaseNames.Flag);
            Term asyncValue = Term.Tuple(flag, Term.A(ErlangBooleanAtoms.False));
            query = fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                guest.Guest.Pid,
                Term.List(
                    Term.A(ProcessInfoNames.Links),
                    flag,
                    Term.A(ProcessInfoNames.Links),
                    flag
                )
            ));
            Require((await query.WaitAsync(DistributionControlWaitHostNames.Deadline)).Equals(Term.List(
                links,
                asyncValue,
                links,
                asyncValue
            )));
            Require(!guest.Unwound.Task.IsCompleted);
            Require(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
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

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessInfoAsyncDistributionHostNames.Failure);
    }
}
