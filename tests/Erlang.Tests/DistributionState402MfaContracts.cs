using Erlang;
using static Erlang.Tests.DistributionState402Names;
using static Erlang.Tests.DistributionState402Access;

namespace Erlang.Tests;

internal static class DistributionState402MfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == ModuleName && (function == CreationFunction && arity == 0 || function == AbortFunction && arity == 2);

    internal static Task Run(string function) => function switch
    {
        CreationFunction => Creation(),
        AbortFunction => AbortPending(),
        _ => throw new ArgumentOutOfRangeException(nameof(function))
    };

    private static async Task Creation()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        DistributionState402Access.Check(!runtime.IsDistributionAlive);
        Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, ModuleName, CreationFunction));
        DistributionState402Access.Check(value.Equals(DistributionState402Atoms.Undefined));
        DistributionState402Access.Check(runtime.Node == ProcessNodeDefaults.Local);
    }

    private static async Task AbortPending()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term first = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ModuleName,
            NewConnection,
            DistributionState402Atoms.Remote
        ));
        using DistributionPendingLease initial = runtime.DistributionPending.Acquire(first);
        DistributionState402Access.Check(initial.Snapshot().State == DistributionPendingState.Pending);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            first
        )).Equals(DistributionState402Atoms.True));
        DistributionState402Access.Check(initial.Snapshot().State == DistributionPendingState.Idle);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            first
        )).Equals(DistributionState402Atoms.True));
        Term current = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ModuleName,
            NewConnection,
            DistributionState402Atoms.Remote
        ));
        using DistributionPendingLease retained = runtime.DistributionPending.Acquire(current);
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            first
        )).Equals(DistributionState402Atoms.True));
        DistributionState402Access.Check(retained.Snapshot().State == DistributionPendingState.Pending);
        await RejectBadArgument(
            actor,
            runtime,
            Term.I(InvalidNodeInteger),
            current
        );
        DistributionState402Access.Check((await Abort(
            actor,
            runtime,
            DistributionState402Atoms.Remote,
            current
        )).Equals(DistributionState402Atoms.True));
        DistributionState402Access.Check(retained.Snapshot().State == DistributionPendingState.Idle);
    }
}
