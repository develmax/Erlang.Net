namespace Erlang.NifNativeBackend;

public static class QueuedWallTimeRepeatChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        _ = await CpuStatisticsAccess.Actor<Term>(runtime,
            async context => await runtime.SetNativeSchedulerWallTime(context, true));
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var submitted = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle blocker = runtime.Spawn(async context =>
        {
            await runtime.ChangeSchedulerBinding(context, Term.I(1));
            entered.TrySetResult();
            if (!release.Wait(CpuStatisticsParameters.Deadline)) throw new TimeoutException();

            return Nil.Value;
        });
        ProcessHandle? requester = null;
        Exception? failure = null;
        try
        {
            await entered.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            requester = runtime.Spawn(async context =>
            {
                try
                {
                    await runtime.ChangeSchedulerBinding(context, Term.I(2));
                    Task<Term> observation = runtime.SetNativeSchedulerWallTime(context, true).AsTask();
                    submitted.TrySetResult(observation);
                    SchedulerWallTimeSamples.Boolean(await observation, true);
                }
                catch (Exception error)
                {
                    submitted.TrySetException(error);
                    throw;
                }

                return Nil.Value;
            });
            Task<Term> actual = await submitted.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            CpuStatisticsAccess.Require(!actual.IsCompleted);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            release.Set();
        }
        try
        {
            if (submitted.Task.IsCompletedSuccessfully)
                SchedulerWallTimeSamples.Boolean(await (await submitted.Task).WaitAsync(CpuStatisticsParameters.Deadline), true);
            CpuStatisticsAccess.Require((await blocker.Completion.WaitAsync(CpuStatisticsParameters.Deadline))
                .Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        try
        {
            if (requester is not null)
                CpuStatisticsAccess.Require((await requester.Completion.WaitAsync(CpuStatisticsParameters.Deadline))
                    .Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        _ = await CpuStatisticsAccess.Actor<Term>(runtime,
            async context => await runtime.SetNativeSchedulerWallTime(context, false));
    }
}
