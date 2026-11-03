namespace Erlang.NifNativeBackend;

public static class MicrostateNestedChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(first, CoreModules.Boolean(false)), false);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(second, CoreModules.Boolean(false)), false);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try
            {
                using (var firstScope = first.Statistics.Microstates.Attach(RuntimeMicrostateNames.Auxiliary, RuntimeMicrostate.Sleep))
                {
                    MicrostateTestAccess.Boolean(MicrostateTestAccess.Set(first, CoreModules.Boolean(true)).GetAwaiter().GetResult(), false);
                    using var firstWork = RuntimeMicrostateExecution.EnterState(first.Statistics.Microstates, RuntimeMicrostate.Other);
                    CpuUserTimeProbe.Burn(work);
                    using (var secondScope = second.Statistics.Microstates.Attach(RuntimeMicrostateNames.Auxiliary, RuntimeMicrostate.Sleep))
                    {
                        MicrostateTestAccess.Boolean(MicrostateTestAccess.Set(second, CoreModules.Boolean(true)).GetAwaiter().GetResult(), false);
                        using var secondWork = RuntimeMicrostateExecution.EnterState(second.Statistics.Microstates, RuntimeMicrostate.Other);
                        var ownBefore = MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(first).GetAwaiter().GetResult());
                        CpuUserTimeProbe.Burn(work);
                        var paused = MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(first).GetAwaiter().GetResult());
                        RuntimeMicrostateRecord firstRecord = firstScope!.Record!;
                        CpuStatisticsAccess.Require(firstRecord.Type == RuntimeMicrostateNames.Auxiliary
                            && MicrostateTestAccess.Row(paused, firstRecord).Counters[RuntimeMicrostateNames.States[(int)RuntimeMicrostate.Other]]
                            == MicrostateTestAccess.Row(ownBefore, firstRecord).Counters[RuntimeMicrostateNames.States[(int)RuntimeMicrostate.Other]]);
                        // The normal rows remain in both COMPLETE public samples; they may advance.
                        CpuStatisticsAccess.Require(ownBefore.Any(value => value.Type == RuntimeMicrostateNames.Scheduler)
                            && paused.Any(value => value.Type == RuntimeMicrostateNames.Scheduler));
                        MicrostateTestAccess.Boolean(MicrostateTestAccess.Set(second, Term.A(MicrostateCaseNames.Reset)).GetAwaiter().GetResult(), true);
                        // Preserve LastCounter: reset during a real active interval does not rebase time.
                        MicrostateSample resetRow = MicrostateTestAccess.Row(
                            MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(second).GetAwaiter().GetResult()), secondScope!.Record!);
                        CpuStatisticsAccess.Require(resetRow.Counters[RuntimeMicrostateNames.States[(int)RuntimeMicrostate.Other]].Sign > 0);
                    }
                }
                CpuStatisticsAccess.Require(RuntimeMicrostateExecution.Current is null);
                var settled = MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(first).GetAwaiter().GetResult());
                CpuUserTimeProbe.Burn(work);
                var host = MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(first).GetAwaiter().GetResult());
                foreach (MicrostateSample old in settled.Where(value => value.Type == RuntimeMicrostateNames.Auxiliary))
                {
                    MicrostateSample same = host.Single(value => value.Type == old.Type && value.Identity == old.Identity);
                    CpuStatisticsAccess.Require(same.Counters[RuntimeMicrostateNames.States[(int)RuntimeMicrostate.Other]]
                        == old.Counters[RuntimeMicrostateNames.States[(int)RuntimeMicrostate.Other]]);
                }
                CpuStatisticsAccess.Require(settled.Any(value => value.Type == RuntimeMicrostateNames.Scheduler)
                    && host.Any(value => value.Type == RuntimeMicrostateNames.Scheduler));
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }
        })
        { IsBackground = true };
        worker.Start();
        Exception? failure = null;
        try
        {
            await completed.Task.WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = error;
        }
        if (!worker.Join(CpuStatisticsParameters.Deadline))
            failure = failure is null ? new TimeoutException() : new AggregateException(failure, new TimeoutException());
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
