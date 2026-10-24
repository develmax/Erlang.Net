using Erlang;
using static Erlang.Tests.NewConnection400HostNames;

namespace Erlang.Tests;

internal static class NewConnection400MfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == ModuleName && function == Function && arity == 1;

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = await runtime.Modules.Call(
                context,
                ModuleName,
                Function,
                NewConnection400HostAtoms.Remote
            );
            if (first is not TupleTerm { Items.Count: 2 } handle || handle.Items[0] is not Integer || handle.Items[1] is not ReferenceTerm)
                throw new InvalidOperationException(Invariant);
            using DistributionPendingLease lease = runtime.DistributionPending.Acquire(first);
            Term repeated = await runtime.Modules.Call(
                context,
                ModuleName,
                Function,
                NewConnection400HostAtoms.Remote
            );
            if (!first.Equals(repeated) || lease.Snapshot().State != DistributionPendingState.Pending)
                throw new InvalidOperationException(Invariant);
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    ModuleName,
                    Function,
                    Nil.Value
                );
                throw new InvalidOperationException(Invariant);
            }
            catch (ErlangException failure) when (failure.Reason.Equals(NewConnection400HostAtoms.BadArgument))
            {
            }

            return true;
        });
    }
}
