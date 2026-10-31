namespace Erlang.NifNativeBackend;

public static class QueuedWallTimeSnapshotChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var runtime = new ProcessRuntime(2);
        _ = await CpuStatisticsAccess.Actor<Term>(runtime,
            async context => await runtime.SetNativeSchedulerWallTime(context, true));
        using var normalRelease = new ManualResetEventSlim();
        using var dirtyRelease = new ManualResetEventSlim();
        var dirtyEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var normalEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var physical = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var submitted = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> dirty = CpuStatisticsAccess.Actor<Term>(
            runtime,
            async context => await runtime.ScheduleDirtyNative419(
                context,
                DirtyNativeKind419.Io,
                CpuStatisticsParameters.DirtyModule,
                CpuStatisticsParameters.DirtyFunction,
                Array.Empty<Term>(),
                (owner, _) =>
            {
                CpuStatisticsAccess.Require(DirtyNativeThread419.Read(owner) == DirtyNativeKind419.Io);
                dirtyEntered.TrySetResult();
                if (!dirtyRelease.Wait(CpuStatisticsParameters.Deadline)) throw new TimeoutException();

                return Nil.Value;
            },
                task => physical.TrySetResult(task)
            )
        );
        ProcessHandle? blocker = null;
        ProcessHandle? requester = null;
        Term? early = null;
        CpuWorkMeasurement elapsed = default;
        Exception? failure = null;
        try
        {
            await Task.WhenAny(dirtyEntered.Task, dirty).WaitAsync(CpuStatisticsParameters.Deadline);
            if (!dirtyEntered.Task.IsCompletedSuccessfully) await dirty;
            blocker = runtime.Spawn(async context =>
            {
                await runtime.ChangeSchedulerBinding(context, Term.I(1));
                normalEntered.TrySetResult();
                if (!normalRelease.Wait(CpuStatisticsParameters.Deadline)) throw new TimeoutException();

                return Nil.Value;
            });
            await normalEntered.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            requester = runtime.Spawn(async context =>
            {
                try
                {
                    await runtime.ChangeSchedulerBinding(context, Term.I(2));
                    Task<Term> observation = runtime.SampleSchedulerWallTime(context, true).AsTask();
                    submitted.TrySetResult(observation);
                    await observation;
                }
                catch (Exception error)
                {
                    submitted.TrySetException(error);
                    throw;
                }

                return Nil.Value;
            });
            Task<Term> sample = await submitted.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            CpuStatisticsAccess.Require(!sample.IsCompleted);
            elapsed = await CpuHostUserWork.Burn(work);
            normalRelease.Set();
            early = await sample.WaitAsync(CpuStatisticsParameters.Deadline);
            var late = SchedulerWallTimeSamples.Read(await CpuStatisticsAccess.Actor<Term>(runtime,
                async context => await runtime.SampleSchedulerWallTime(context, true)));
            var before = SchedulerWallTimeSamples.Read(early);
            int firstIo = runtime.ActorDispatchCapacity + DirtyNativeSchedulerNames419.WorkersPerKind + 1;
            long delta = late.Where(pair => pair.Key >= firstIo).Sum(pair => pair.Value.Active - before[pair.Key].Active);
            CpuStatisticsAccess.Require(delta * TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency >= elapsed.UserTicks / 2);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            normalRelease.Set();
            dirtyRelease.Set();
        }
        try
        {
            await dirty.WaitAsync(CpuStatisticsParameters.Deadline);
            await (await physical.Task.WaitAsync(CpuStatisticsParameters.Deadline)).WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        foreach (ProcessHandle? actor in new[] { blocker, requester })
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
        _ = await CpuStatisticsAccess.Actor<Term>(runtime,
            async context => await runtime.SetNativeSchedulerWallTime(context, false));
    }
}
