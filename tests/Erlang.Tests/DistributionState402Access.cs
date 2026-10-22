using Erlang;
using static Erlang.Tests.DistributionState402Names;

namespace Erlang.Tests;

internal static class DistributionState402Access
{
    internal static Task<Term> Abort(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        Term node,
        Term handle
    ) => actor.InvokeAsync(async context => await runtime.Modules.Call(
        context,
        ModuleName,
        AbortFunction,
        node,
        handle
    ));

    internal static async Task RejectBadArgument(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        Term node,
        Term handle
    )
    {
        try
        {
            await Abort(
                actor,
                runtime,
                node,
                handle
            );
            throw new InvalidOperationException(Failure);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(DistributionState402Atoms.BadArgument))
        {
            Check(failure.ExceptionClass == ErlangExceptionClasses.Error);
            Check(failure.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } });
            var frame = (TupleTerm)((Cons)failure.StackTraceTerm).Head;
            Check(frame.Items[0].Equals(DistributionState402Atoms.Module));
            Check(frame.Items[1].Equals(DistributionState402Atoms.Abort));
            Check(frame.Items[2].Equals(Term.List(node, handle)));
            // Canonical native dispatch supplies its formatter metadata on direct HOST calls too.
            Check(frame.Items[3].Equals(DistributionState402Atoms.NativeInfo));
        }
    }

    internal static async Task SettleKilled(Task<bool> checkpoint, Task<Term> completion)
    {
        Check((await completion.WaitAsync(Deadline)).Equals(DistributionState402Atoms.Killed));
        try
        {
            await checkpoint.WaitAsync(Deadline);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ErlangException failure) when (failure.Reason.Equals(DistributionState402Atoms.Killed))
        {
        }
    }

    internal static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
