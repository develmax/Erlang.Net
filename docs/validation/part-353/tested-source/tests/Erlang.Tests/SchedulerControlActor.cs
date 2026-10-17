namespace Erlang.Tests;

internal sealed class SchedulerControlActor
{
    internal TaskCompletionSource<ProcessContext> Entered { get; } = SchedulerControlCalls.Signal<ProcessContext>();
    internal ProcessHandle Process { get; }

    internal SchedulerControlActor(ProcessRuntime runtime, int lane, Func<ProcessContext, ValueTask<Term>> body)
    {
        Process = runtime.Spawn(async context =>
        {
            await SchedulerControlCalls.Bind(context, lane);
            Entered.TrySetResult(context);

            return await body(context);
        });
    }

    internal Task<ProcessContext> Context() => Entered.Task.WaitAsync(SchedulerControlCalls.Deadline);
}
