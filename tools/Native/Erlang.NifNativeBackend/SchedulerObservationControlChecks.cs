namespace Erlang.NifNativeBackend;

public static class SchedulerObservationControlChecks
{
    public static Task Native() => Run(SchedulerObservationControlMode.NativeChange);

    public static Task Gather() => Run(SchedulerObservationControlMode.Gather);

    public static Task Managed() => Run(SchedulerObservationControlMode.ManagedFlag);

    private static async Task Run(SchedulerObservationControlMode mode)
    {
        await using var runtime = new ProcessRuntime(SchedulerObservationControlCaseNames.Capacity);
        if (mode == SchedulerObservationControlMode.Gather)
            _ = await CpuStatisticsAccess.Actor<Term>(runtime,
                async context => await runtime.SetNativeSchedulerWallTime(context, true));
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var submitted = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var info = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle target = runtime.SpawnTraced(
            context =>
        {
            CpuStatisticsAccess.Require(RuntimeActorExecutionTurn.LaneIdentity == SchedulerObservationControlCaseNames.TargetLane);
            entered.TrySetResult();
            if (!release.Wait(CpuStatisticsParameters.Deadline)) throw new TimeoutException();

            return ValueTask.FromResult<Term>(Nil.Value);
        },
            null,
            false,
            null,
            schedulerBinding: SchedulerObservationControlCaseNames.TargetLane
        );
        ProcessHandle? query = null;
        ProcessHandle? inspector = null;
        Exception? failure = null;
        try
        {
            await entered.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            query = runtime.SpawnTraced(
                async context =>
            {
                try
                {
                    Task<Term> operation = mode switch
                    {
                        SchedulerObservationControlMode.NativeChange => runtime.SetNativeSchedulerWallTime(context, true).AsTask(),
                        SchedulerObservationControlMode.Gather => runtime.SampleSchedulerWallTime(context, true).AsTask(),
                        SchedulerObservationControlMode.ManagedFlag => runtime.SetPublicSchedulerWallTime(context, CoreModules.Boolean(true)).AsTask(),
                        _ => throw new ArgumentOutOfRangeException(nameof(mode))
                    };
                    submitted.TrySetResult(operation);
                    await operation;
                }
                catch (Exception error)
                {
                    submitted.TrySetException(error);
                    throw;
                }

                return Nil.Value;
            },
                null,
                false,
                null,
                schedulerBinding: SchedulerObservationControlCaseNames.QueryLane
            );
            Task<Term> actual = await submitted.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            CpuStatisticsAccess.Require(!actual.IsCompleted);
            Pid queryPid = query.Pid;
            inspector = runtime.SpawnTraced(
                async context =>
            {
                try
                {
                    ProcessInfoItem[] keys = [
                        ProcessInfoItem.Parse(Term.A(ProcessInfoNames.Status)),
                        ProcessInfoItem.Parse(Term.A(ProcessInfoNames.CurrentFunction)),
                        ProcessInfoItem.Parse(Term.A(ProcessInfoNames.CurrentStackTrace))
                    ];
                    info.TrySetResult(await runtime.QueryProcessInfo(
                        context,
                        queryPid,
                        keys,
                        false
                    ));
                }
                catch (Exception error)
                {
                    info.TrySetException(error);
                    throw;
                }

                return Nil.Value;
            },
                null,
                false,
                null,
                schedulerBinding: SchedulerObservationControlCaseNames.InspectorLane
            );
            Term snapshot = await info.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            TupleTerm status = Cons.Items(snapshot).Cast<TupleTerm>()
                .Single(item => item.Items[0].Equals(Term.A(ProcessInfoNames.Status)));
            CpuStatisticsAccess.Require(status.Items[1].Equals(Term.A(ProcessInfoStatusNames.Waiting)));
            // Owner inspection completed while the actual target normal turn is
            // STILL blocked. This cannot be satisfied by a fabricated ACK/snapshot.
            CpuStatisticsAccess.Require(!release.IsSet && !actual.IsCompleted);
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
                await (await submitted.Task).WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        foreach (ProcessHandle? actor in new[] { target, query, inspector })
        {
            try
            {
                if (actor is not null)
                    CpuStatisticsAccess.Require((await actor.Completion.WaitAsync(CpuStatisticsParameters.Deadline))
                        .Equals(Term.A(ProcessExitReasons.Normal)));
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
