namespace Erlang.NifNativeBackend;

public static class MicrostateProducerChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var runtime = new ProcessRuntime(2);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), false);
        MicrostateSample[] before = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        _ = await CpuStatisticsAccess.Actor(runtime, _ => ValueTask.FromResult(CpuUserTimeProbe.Burn(work)));
        MicrostateSample[] normal = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(normal, MicrostateCaseNames.Emulator)
            > MicrostateTestAccess.Sum(before, MicrostateCaseNames.Emulator));
        RuntimeSchedulerAuxiliaryBatch420 batch = CpuStatisticsAccess.Dispatcher(runtime)
            .PrepareAuxiliary420(_ => CpuUserTimeProbe.Burn(work));
        batch.Publish();
        await batch.Completion.WaitAsync(CpuStatisticsParameters.Deadline);
        MicrostateSample[] aux = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(aux, MicrostateCaseNames.Aux)
            > MicrostateTestAccess.Sum(normal, MicrostateCaseNames.Aux));
        var physical = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? failure = null;
        try
        {
            _ = await CpuStatisticsAccess.Actor<Term>(
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
                    CpuUserTimeProbe.Burn(work);

                    return Nil.Value;
                },
                    task => physical.TrySetResult(task)
                )
            );
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            if (failure is null || physical.Task.IsCompletedSuccessfully)
                await (await physical.Task.WaitAsync(CpuStatisticsParameters.Deadline)).WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        MicrostateSample[] dirty = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(dirty, MicrostateCaseNames.Emulator, RuntimeMicrostateNames.DirtyIo).Sign > 0);
        _ = await CpuHostUserWork.Burn(work);
        MicrostateSample[] idle = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(idle, MicrostateCaseNames.Sleep, RuntimeMicrostateNames.DirtyIo)
            > MicrostateTestAccess.Sum(dirty, MicrostateCaseNames.Sleep, RuntimeMicrostateNames.DirtyIo));
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(false)), true);
    }
}
