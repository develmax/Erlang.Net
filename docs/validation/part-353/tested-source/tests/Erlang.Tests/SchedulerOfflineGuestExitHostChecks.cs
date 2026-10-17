namespace Erlang.Tests;

internal static class SchedulerOfflineGuestExitHostChecks
{
    internal static async Task PendingKillIsAdmittedOnlyAfterLaneReturnsOnline()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        var receiving = SchedulerControlCalls.Signal();
        var finallyLane = SchedulerControlCalls.Signal<int>();
        var offline = SchedulerControlCalls.Signal();
        var owner = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.Second,
            async context =>
        {
            try
            {
                ValueTask<Term?> pending = context.ReceiveAsync<Term>(value => value);
                SchedulerControlCalls.Require(!pending.IsCompleted);
                receiving.TrySetResult();
                await pending;

                return Nil.Value;
            }
            finally
            {
                finallyLane.TrySetResult(SchedulerControlInspection.ActualLane());
            }
        }
        );
        ProcessContext captured = await owner.Context();
        await receiving.Task.WaitAsync(SchedulerControlCalls.Deadline);
        var controller = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.First,
            async context =>
        {
            await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First);
            offline.TrySetResult();
            await context.ReceiveAsync<Term>(value => value);

            return Nil.Value;
        }
        );
        await offline.Task.WaitAsync(SchedulerControlCalls.Deadline);
        await SchedulerControlCalls.Kill(runtime, owner.Process.Pid);
        await SchedulerControlInspection.Wait(() => SchedulerControlInspection.Ready(captured));
        SchedulerControlCalls.Require(!captured.Cancellation.IsCancellationRequested && !owner.Process.Completion.IsCompleted &&
            !finallyLane.Task.IsCompleted);
        var restore = new SchedulerControlActor(
            runtime,
            SchedulerAffinityHostNames.First,
            async context =>
        {
            SchedulerControlCalls.Require((await SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second))
                .Equals(Term.I(SchedulerAffinityHostNames.First)));

            return Nil.Value;
        }
        );
        await SchedulerControlCalls.Normal(restore.Process);
        SchedulerControlCalls.Require((await owner.Process.Completion.WaitAsync(SchedulerControlCalls.Deadline))
            .Equals(Term.A(SchedulerControlNames.Killed)) && captured.Cancellation.IsCancellationRequested);
        SchedulerControlCalls.Require(await finallyLane.Task.WaitAsync(SchedulerControlCalls.Deadline) ==
            SchedulerAffinityHostNames.Second);
        await runtime.DisposeAsync().AsTask().WaitAsync(SchedulerControlCalls.Deadline);
        await SchedulerControlInspection.Completion(runtime).WaitAsync(SchedulerControlCalls.Deadline);
        SchedulerControlCalls.Require(controller.Process.Completion.IsCompleted);
    }
}
