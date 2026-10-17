namespace Erlang.Tests;

internal static class SchedulerOfflineRetirementHostChecks
{
    internal static async Task RunningParksPendingGuestKillAndRetirementSeparatelyJoinsCleanup()
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
                ValueTask<Term?> receive = context.ReceiveAsync<Term>(value => value);
                SchedulerControlCalls.Require(!receive.IsCompleted);
                receiving.TrySetResult();
                await receive;

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
        SchedulerControlCalls.Require(!finallyLane.Task.IsCompleted);
        SchedulerControlCalls.Require(SchedulerControlInspection.Snapshot(runtime).Online == SchedulerAffinityHostNames.First);
        SchedulerControlCalls.Require(!captured.Cancellation.IsCancellationRequested && !owner.Process.Completion.IsCompleted);
        await runtime.DisposeAsync().AsTask().WaitAsync(SchedulerControlCalls.Deadline);
        await SchedulerControlInspection.Completion(runtime).WaitAsync(SchedulerControlCalls.Deadline);
        SchedulerControlCalls.Require(await finallyLane.Task.WaitAsync(SchedulerControlCalls.Deadline) ==
            SchedulerAffinityHostNames.Second);
        SchedulerControlCalls.Require((await owner.Process.Completion.WaitAsync(SchedulerControlCalls.Deadline))
            .Equals(Term.A(SchedulerControlNames.Shutdown)));
        SchedulerControlCalls.Require(controller.Process.Completion.IsCompleted);
    }
}
