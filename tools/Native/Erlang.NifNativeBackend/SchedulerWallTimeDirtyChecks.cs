namespace Erlang.NifNativeBackend;

public static class SchedulerWallTimeDirtyChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 4);
        await using var observer = new SchedulerWallTimeTestActor(runtime);
        SchedulerWallTimeSamples.Boolean(await observer.Flag(true), false);
        var initial = SchedulerWallTimeSamples.Read(await observer.Sample(true));
        var regular = SchedulerWallTimeSamples.Read(await observer.Sample());
        int normalCount = runtime.ActorDispatchCapacity;
        int ioCount = checked((int)((Integer)await observer.Call(SequenceTraceNames.SystemInfo,
            Term.A(DirtyNativeSchedulerNames419.Io))).Value);
        CpuStatisticsAccess.Require(regular.Count == normalCount + DirtyNativeSchedulerNames419.WorkersPerKind
            && initial.Count == regular.Count + ioCount);
        CpuStatisticsAccess.Require(initial.Keys.Order().SequenceEqual(Enumerable.Range(1, initial.Count)));
        RuntimeManagedSchedulerWallTimeSnapshot normalBefore = runtime.CaptureManagedSchedulerWallTime();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var physical = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> invocation = CpuStatisticsAccess.Actor<Term>(
            runtime,
            async context =>
            await runtime.ScheduleDirtyNative419(
                context,
                DirtyNativeKind419.Io,
                CpuStatisticsParameters.DirtyModule,
                CpuStatisticsParameters.DirtyFunction,
                Array.Empty<Term>(),
                (owner, _) =>
                {
                    CpuStatisticsAccess.Require(DirtyNativeThread419.Read(owner) == DirtyNativeKind419.Io);
                    entered.TrySetResult();
                    if (!release.Wait(CpuStatisticsParameters.Deadline)) throw new TimeoutException();

                    return Nil.Value;
                },
                task => physical.TrySetResult(task)
            )
        );
        Exception? failure = null;
        try
        {
            await Task.WhenAny(entered.Task, invocation).WaitAsync(CpuStatisticsParameters.Deadline);
            if (!entered.Task.IsCompletedSuccessfully) await invocation;
            var before = SchedulerWallTimeSamples.Read(await observer.Sample(true));
            CpuWorkMeasurement measured = await CpuHostUserWork.Burn(work);
            var during = SchedulerWallTimeSamples.Read(await observer.Sample(true));
            int firstIo = normalCount + DirtyNativeSchedulerNames419.WorkersPerKind + 1;
            long delta = during.Where(pair => pair.Key >= firstIo).Sum(pair => pair.Value.Active - before[pair.Key].Active);
            CpuStatisticsAccess.Require(delta * TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency >= measured.UserTicks / 2);
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
            await invocation.WaitAsync(CpuStatisticsParameters.Deadline);
            await (await physical.Task.WaitAsync(CpuStatisticsParameters.Deadline)).WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        var settled = SchedulerWallTimeSamples.Read(await observer.Sample(true));
        SchedulerWallTimeSamples.Boolean(await observer.Flag(false), true);
        SchedulerWallTimeSamples.Boolean(await observer.Flag(true), false);
        var resumed = SchedulerWallTimeSamples.Read(await observer.Sample(true));
        RuntimeManagedSchedulerWallTimeSnapshot normalAfter = runtime.CaptureManagedSchedulerWallTime();
        CpuStatisticsAccess.Require(normalAfter.Generation > normalBefore.Generation);
        foreach (var pair in settled.Where(pair => pair.Key > normalCount))
            CpuStatisticsAccess.Require(resumed[pair.Key].Active >= pair.Value.Active && resumed[pair.Key].Total >= pair.Value.Total);
        SchedulerWallTimeSamples.Boolean(await observer.Flag(false), true);
    }
}
