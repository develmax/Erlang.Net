using System.Runtime.ExceptionServices;
using static Erlang.Tests.RegexForeignEngine605Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class RegexForeignEngine605Checks
{
    internal static Task DirectForeignActorDispose() => RegexCustody597Fixture.Run(async fixture =>
    {
        var exported = (ResourceReferenceTerm)RegexLifecycle599Checks.InvokeStatic(
            typeof(ProcessRuntime).Assembly.GetType(RegexCustody597Names.RegexRoots, true)!,
            RegexLifecycle599Names.HostCopy,
            fixture.Reference,
            null
        )!;
        using var host = (IDisposable)Property(exported, RegexCustody597Names.HostField);
        // Keep a real compiled backing alive without waiting on this test's producer.
        fixture.Release();
        await using var caller = new ProcessRuntime(1);
        Task? closing = null;
        Exception? failure = null;
        bool returned = false;
        ProcessHandle actor = caller.Spawn(context =>
        {
            try
            {
                if (TaskScheduler.Current == TaskScheduler.Default && SynchronizationContext.Current is null)
                    throw new InvalidOperationException(MissingScheduler);
                closing = fixture.Engine!.DisposeAsync().AsTask();
                closing.WaitAsync(JoinDeadline).GetAwaiter().GetResult();
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
            await actor.Completion.WaitAsync(CompletionDeadline);
        }
        finally
        {
            // On old code, the bounded synchronous join exits the foreign
            // actor, allowing its wrongly captured cleanup continuation to run.
            if (closing is not null) await closing.WaitAsync(CompletionDeadline);
            else await fixture.Engine!.DisposeAsync();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        if (!returned || closing is null || !closing.IsCompletedSuccessfully)
            throw new InvalidOperationException(Incomplete);
        if (!(bool)Property(fixture.Backing!, RegexCustody597Names.Freed))
            throw new InvalidOperationException(Incomplete);
        RegexNative411Access.Empty(fixture.Engine!);
        GC.KeepAlive(exported);
    });
}
