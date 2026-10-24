using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

public static class SchedulerStatisticsHostChecks
{
    public static async Task Run()
    {
        await QueuedOwnerOnce();
        await OfflineLaneOrder();
        await InitializerExcluded();
    }

    public static async Task QueuedOwnerOnce()
    {
        await using var runtime = new ProcessRuntime(1);
        var ready = new TaskCompletionSource<TaskScheduler>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle? child = null;
        var controller = runtime.Spawn(async context =>
        {
            Require((await Sample(context, RuntimeStatisticsNames.RunQueue)).Equals(Term.I(0)));
            Require((await Sample(context, RuntimeStatisticsNames.ActiveTasks)).Equals(Term.List(Term.I(1), Term.I(0))));
            child = runtime.Spawn(async _ =>
            {
                ready.TrySetResult(TaskScheduler.Current);
                await release.Task;

                return Nil.Value;
            });
            TaskScheduler scheduler = await ready.Task;
            var callbacks = new[]
            {
                Task.Factory.StartNew(
                    () => { },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler
                ),
                Task.Factory.StartNew(
                    () => { },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler
                )
            };
            Require((await Sample(context, RuntimeStatisticsNames.RunQueue)).Equals(Term.I(1)));
            Require((await Sample(context, RuntimeStatisticsNames.RunQueueLengths)).Equals(Term.List(Term.I(1), Term.I(0))));
            Require((await Sample(context, RuntimeStatisticsNames.RunQueueLengthsAll)).Equals(Term.List(Term.I(1), Term.I(0), Term.I(0))));
            Require((await Sample(context, RuntimeStatisticsNames.ActiveTasks)).Equals(Term.List(Term.I(2), Term.I(0))));
            Require((await Sample(context, RuntimeStatisticsNames.ActiveTasksAll)).Equals(Term.List(Term.I(2), Term.I(0), Term.I(0))));
            Require((await Sample(context, RuntimeStatisticsNames.TotalRunQueueLengths)).Equals(Term.I(1)));
            Require((await Sample(context, RuntimeStatisticsNames.TotalRunQueueLengthsAll)).Equals(Term.I(1)));
            Require((await Sample(context, RuntimeStatisticsNames.TotalActiveTasks)).Equals(Term.I(2)));
            Require((await Sample(context, RuntimeStatisticsNames.TotalActiveTasksAll)).Equals(Term.I(2)));
            await Task.WhenAll(callbacks);
            Require((await Sample(context, RuntimeStatisticsNames.RunQueue)).Equals(Term.I(0)));
            Require((await Sample(context, RuntimeStatisticsNames.TotalActiveTasks)).Equals(Term.I(1)));
            release.TrySetResult();

            return Nil.Value;
        });
        try
        {
            Require((await controller.Completion.WaitAsync(SchedulerStatisticsHostNames.Deadline)).Equals(Term.A(SchedulerStatisticsHostNames.Normal)));
            Require(child is not null);
            Require((await child!.Completion.WaitAsync(SchedulerStatisticsHostNames.Deadline)).Equals(Term.A(SchedulerStatisticsHostNames.Normal)));
        }
        finally
        {
            release.TrySetResult();
        }
    }

    public static async Task OfflineLaneOrder()
    {
        await using var runtime = new ProcessRuntime(2);
        int executions = 0;
        var controller = runtime.Spawn(async context =>
        {
            await runtime.Modules.Call(
                context,
                SchedulerStatisticsHostNames.Erlang,
                SchedulerStatisticsHostNames.Flag,
                Term.A(SchedulerStatisticsHostNames.Scheduler),
                Term.I(1)
            );
            await runtime.Modules.Call(
                context,
                SchedulerStatisticsHostNames.Erlang,
                SchedulerStatisticsHostNames.SystemFlag,
                Term.A(SchedulerStatisticsHostNames.Online),
                Term.I(1)
            );
            var callback = new FunctionTerm(0, (_, _) =>
            {
                Interlocked.Increment(ref executions);

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            var started = (TupleTerm)await runtime.Modules.Call(
                context,
                SchedulerStatisticsHostNames.Erlang,
                SchedulerStatisticsHostNames.Spawn,
                callback,
                Term.List(Term.A(SchedulerStatisticsHostNames.Monitor),
                    Term.Tuple(Term.A(SchedulerStatisticsHostNames.Scheduler), Term.I(2)))
            );
            Require(Volatile.Read(ref executions) == 0);
            Require((await Sample(context, RuntimeStatisticsNames.RunQueueLengths)).Equals(Term.List(Term.I(0), Term.I(1), Term.I(0))));
            Require((await Sample(context, RuntimeStatisticsNames.ActiveTasksAll)).Equals(Term.List(
                Term.I(1),
                Term.I(1),
                Term.I(0),
                Term.I(0)
            )));
            await runtime.Modules.Call(
                context,
                SchedulerStatisticsHostNames.Erlang,
                SchedulerStatisticsHostNames.SystemFlag,
                Term.A(SchedulerStatisticsHostNames.Online),
                Term.I(2)
            );
            Term? down = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: SchedulerStatisticsHostNames.Down }, _, _, _, _] } ? value : null,
                SchedulerStatisticsHostNames.Deadline
            );
            Require(down is TupleTerm message && message.Items[1].Equals(started.Items[1]) && message.Items[3].Equals(started.Items[0]) &&
                message.Items[4].Equals(Term.A(SchedulerStatisticsHostNames.Normal)));
            Require(Volatile.Read(ref executions) == 1);

            return Nil.Value;
        });
        Require((await controller.Completion.WaitAsync(SchedulerStatisticsHostNames.Deadline)).Equals(Term.A(SchedulerStatisticsHostNames.Normal)));
    }

    public static async Task InitializerExcluded()
    {
        await using var runtime = new ProcessRuntime(1);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Gate, flags)!.GetValue(runtime)!;
        var processes = (IDictionary)typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Processes, flags)!.GetValue(runtime)!;
        ProcessContext initial;
        lock (gate)
            initial = (ProcessContext)processes[runtime.InitializationPid]!;
        await runtime.InitializeModulesAsync(_ =>
        {
            ValueTask<Term> queued = Sample(initial, RuntimeStatisticsNames.RunQueue);
            ValueTask<Term> active = Sample(initial, RuntimeStatisticsNames.TotalActiveTasks);
            ValueTask<Term> lanes = Sample(initial, RuntimeStatisticsNames.ActiveTasksAll);
            Require(queued.IsCompletedSuccessfully && queued.Result.Equals(Term.I(0)));
            Require(active.IsCompletedSuccessfully && active.Result.Equals(Term.I(0)));
            Require(lanes.IsCompletedSuccessfully && lanes.Result.Equals(Term.List(Term.I(0), Term.I(0), Term.I(0))));
        }).AsTask().WaitAsync(SchedulerStatisticsHostNames.Deadline);
    }

    private static ValueTask<Term> Sample(ProcessContext context, string key) => context.Runtime.Modules.Call(
        context,
        SchedulerStatisticsHostNames.Erlang,
        RuntimeStatisticsNames.Function,
        Term.A(key)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SchedulerStatisticsHostNames.Failure);
    }
}
