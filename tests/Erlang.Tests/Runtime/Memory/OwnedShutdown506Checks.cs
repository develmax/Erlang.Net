using static Erlang.Tests.OwnedShutdown506Names;

namespace Erlang.Tests;

internal static class OwnedShutdown506Checks
{
    internal static async Task NestedForeignInvocation()
    {
        await using var first = OwnedGuest506RuntimeFactory.Create();
        await using var second = OwnedGuest506RuntimeFactory.Create();
        await using var unrelated = OwnedGuest506RuntimeFactory.Create();
        await using var firstActor = new RuntimeNodeFoundationHostActor(first);
        await using var secondActor = new RuntimeNodeFoundationHostActor(second);
        ProcessContext secondContext = await secondActor.Ready;
        bool rejected = false, unrelatedClosed = false;
        Task? admittedShutdown = null;
        Task? capturedShutdown = null;
        var releaseCaptured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        second.Modules.Register(
            Module,
            Inner,
            0,
            async (_, _) =>
        {
            // A runtime absent from the active chain may still shut down.
            await unrelated.DisposeAsync();
            unrelatedClosed = true;
            capturedShutdown = Task.Run(async () =>
            {
                await releaseCaptured.Task;
                // Both invocation objects are disposed by the time the host
                // releases this captured context; they must not block shutdown.
                await first.DisposeAsync();
            });
            try
            {
                Task shutdown = first.DisposeAsync().AsTask();
                if (!shutdown.IsCompleted)
                {
                    // Awaiting here would make the cycle permanent. Preserve
                    // the real pending operation and let both frames unwind.
                    admittedShutdown = shutdown;

                    return Term.A(Pending);
                }
                await shutdown;
            }
            catch (InvalidOperationException)
            {
                rejected = true;

                return Term.A(Rejected);
            }

            return Term.A(Pending);
        }
        );
        first.Modules.Register(
            Module,
            Outer,
            0,
            async (_, _) =>
            await second.Modules.Call(secondContext, Module, Inner)
        );
        Exception? failure = null;
        try
        {
            Term observed = await firstActor.InvokeAsync(context =>
                first.Modules.Call(context, Module, Outer).AsTask()).WaitAsync(Deadline);
            if (!observed.Equals(Term.A(Rejected)))
                failure = new InvalidOperationException(Failure);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            await firstActor.DisposeAsync();
            await secondActor.DisposeAsync();
            releaseCaptured.TrySetResult();
            if (capturedShutdown is not null)
                await capturedShutdown.WaitAsync(Deadline);
            if (admittedShutdown is not null)
                await admittedShutdown.WaitAsync(Deadline);
        }
        if (!rejected || !unrelatedClosed || admittedShutdown is not null || failure is not null)
            throw new InvalidOperationException(Failure, failure);
    }
}
