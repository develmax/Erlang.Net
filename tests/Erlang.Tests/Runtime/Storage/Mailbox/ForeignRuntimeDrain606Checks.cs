using System.Runtime.ExceptionServices;
using static Erlang.Tests.ForeignRuntimeDrain606Names;

namespace Erlang.Tests;

internal static class ForeignRuntimeDrain606Checks
{
    internal static async Task Run()
    {
        await using var caller = new ProcessRuntime(1);
        var foreign = new ProcessRuntime(1);
        Exception? failure = null;
        bool returned = false;
        Task? disposal = null;
        ProcessHandle actor = caller.Spawn(context =>
        {
            try
            {
                // Real runtime actor scheduler; this synchronous join matches
                // the existing native503/504 invalid-owner controls.
                if (TaskScheduler.Current == TaskScheduler.Default && SynchronizationContext.Current is null)
                    throw new InvalidOperationException(MissingActorScheduler);
                disposal = foreign.DisposeAsync().AsTask();
                disposal.WaitAsync(Deadline).GetAwaiter().GetResult();
                returned = true;
            }
            catch (Exception error)
            {
                failure = error;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        try
        {
            // On old code the bounded synchronous wait faults, then returning
            // from the actor allows its captured continuation to recover.
            await actor.Completion.WaitAsync(CompletionDeadline);
        }
        finally
        {
            if (disposal is not null) await disposal.WaitAsync(CompletionDeadline);
            else await foreign.DisposeAsync();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        if (!returned || disposal is null || !disposal.IsCompletedSuccessfully)
            throw new InvalidOperationException(Incomplete);
    }
}
