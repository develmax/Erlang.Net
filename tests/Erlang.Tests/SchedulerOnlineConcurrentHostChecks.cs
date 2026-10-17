namespace Erlang.Tests;

internal static class SchedulerOnlineConcurrentHostChecks
{
    internal static async Task ParkedGrantedHeadDefersGuestKillUntilAdministrativeRetirement()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        using var releaseLane = new ManualResetEventSlim();
        var held = SchedulerControlCalls.Signal();
        var firstResult = SchedulerControlCalls.Signal<Term>();
        var successorResult = SchedulerControlCalls.Signal<Term>();
        int headApplied = 0;
        var head = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.Second,
            async context =>
        {
            held.TrySetResult();
            SchedulerControlCalls.Require(releaseLane.Wait(SchedulerControlCalls.Deadline, context.Cancellation));
            await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second);
            Interlocked.Increment(ref headApplied);

            return Nil.Value;
        }
        );
        try
        {
            await held.Task.WaitAsync(SchedulerControlCalls.Deadline);
            var first = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                async context =>
            {
                firstResult.TrySetResult(await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First));

                return Nil.Value;
            }
            );
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime) is var state &&
                state.Online == SchedulerAffinityHostNames.First && Equals(state.Changing, first.Process.Pid));
            releaseLane.Set();
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime) is var state &&
                state.Changing is null && Equals(state.Granted, head.Process.Pid));
            var successor = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                async context =>
            {
                successorResult.TrySetResult(await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second));

                return Nil.Value;
            }
            );
            await successor.Context();
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime).Queued ==
                SchedulerControlNames.TwoQueuedRequests);
            SchedulerControlCalls.Require(!successorResult.Task.IsCompleted && Volatile.Read(ref headApplied) ==
                SchedulerControlNames.NoRequests);
            SchedulerControlCalls.Require(SchedulerControlInspection.Snapshot(runtime).Online == SchedulerAffinityHostNames.First);
            ProcessContext headContext = await head.Context();
            await SchedulerControlCalls.Kill(runtime, head.Process.Pid);
            SchedulerControlCalls.Require(!headContext.Cancellation.IsCancellationRequested && !head.Process.Completion.IsCompleted);
            SchedulerControlCalls.Require(!successorResult.Task.IsCompleted &&
                Equals(SchedulerControlInspection.Snapshot(runtime).Granted, head.Process.Pid));
            SchedulerControlCalls.Require((await firstResult.Task.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.I(SchedulerAffinityHostNames.Second)));
            await SchedulerControlCalls.Normal(first.Process);
            // A public online change cannot bypass the reserved offline head.
            // Host retirement is a separate operation; it is not a guest kill.
            await runtime.DisposeAsync().AsTask().WaitAsync(SchedulerControlCalls.Deadline);
            await SchedulerControlInspection.Completion(runtime).WaitAsync(SchedulerControlCalls.Deadline);
            SchedulerControlCalls.Require((await head.Process.Completion.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.A(SchedulerControlNames.Shutdown)));
            SchedulerControlCalls.Require(Volatile.Read(ref headApplied) == SchedulerControlNames.NoRequests);
        }
        finally
        {
            releaseLane.Set();
        }
    }

    internal static async Task ActiveCancellationCannotAdvanceBeforeQuiescence()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        using var releaseLane = new ManualResetEventSlim();
        var held = SchedulerControlCalls.Signal();
        var successorResult = SchedulerControlCalls.Signal<Term>();
        var blocker = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.Second,
            context =>
        {
            held.TrySetResult();
            SchedulerControlCalls.Require(releaseLane.Wait(SchedulerControlCalls.Deadline, context.Cancellation));

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        try
        {
            await held.Task.WaitAsync(SchedulerControlCalls.Deadline);
            var changing = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                context => SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First)
            );
            await SchedulerControlInspection.Wait(() => Equals(SchedulerControlInspection.Snapshot(runtime).Changing,
                changing.Process.Pid));
            var successor = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                async context =>
            {
                successorResult.TrySetResult(await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second));

                return Nil.Value;
            }
            );
            await successor.Context();
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime).Queued ==
                SchedulerControlNames.OneQueuedRequest);
            ProcessContext changingContext = await changing.Context();
            await SchedulerControlCalls.Kill(runtime, changing.Process.Pid);
            SchedulerControlCalls.Require((await changing.Process.Completion.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.A(SchedulerControlNames.Killed)) && changingContext.Cancellation.IsCancellationRequested);
            var state = SchedulerControlInspection.Snapshot(runtime);
            SchedulerControlCalls.Require(Equals(state.Changing, changing.Process.Pid) && state.Granted is null &&
                state.Online == SchedulerAffinityHostNames.First && !successorResult.Task.IsCompleted);
            releaseLane.Set();
            SchedulerControlCalls.Require((await successorResult.Task.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.I(SchedulerAffinityHostNames.First)));
            await SchedulerControlCalls.Normal(blocker.Process);
            await SchedulerControlCalls.Normal(successor.Process);
        }
        finally
        {
            releaseLane.Set();
        }
    }

    internal static async Task CanceledQueuedWaiterDoesNotStallSuccessor()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        using var releaseLane = new ManualResetEventSlim();
        var held = SchedulerControlCalls.Signal();
        var successorResult = SchedulerControlCalls.Signal<Term>();
        var blocker = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.Second,
            context =>
        {
            held.TrySetResult();
            SchedulerControlCalls.Require(releaseLane.Wait(SchedulerControlCalls.Deadline, context.Cancellation));

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        try
        {
            await held.Task.WaitAsync(SchedulerControlCalls.Deadline);
            var changing = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                context => SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First)
            );
            await SchedulerControlInspection.Wait(() => Equals(SchedulerControlInspection.Snapshot(runtime).Changing,
                changing.Process.Pid));
            var canceled = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                context => SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second)
            );
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime).Queued ==
                SchedulerControlNames.OneQueuedRequest);
            var successor = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                async context =>
            {
                successorResult.TrySetResult(await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First));

                return Nil.Value;
            }
            );
            await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Snapshot(runtime).Queued ==
                SchedulerControlNames.TwoQueuedRequests);
            ProcessContext canceledContext = await canceled.Context();
            await SchedulerControlCalls.Kill(runtime, canceled.Process.Pid);
            SchedulerControlCalls.Require((await canceled.Process.Completion.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.A(SchedulerControlNames.Killed)) && canceledContext.Cancellation.IsCancellationRequested);
            SchedulerControlCalls.Require(SchedulerControlInspection.Snapshot(runtime) is var state &&
                Equals(state.Changing, changing.Process.Pid) && state.Queued == SchedulerControlNames.OneQueuedRequest &&
                !successorResult.Task.IsCompleted);
            releaseLane.Set();
            SchedulerControlCalls.Require((await successorResult.Task.WaitAsync(SchedulerControlCalls.Deadline))
                .Equals(Term.I(SchedulerAffinityHostNames.First)));
            await SchedulerControlCalls.Normal(changing.Process);
            await SchedulerControlCalls.Normal(successor.Process);
            await SchedulerControlCalls.Normal(blocker.Process);
        }
        finally
        {
            releaseLane.Set();
        }
    }
}
