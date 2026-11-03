namespace Erlang.NifNativeBackend;

public static class CpuStatisticsInFlightChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 2);
        _ = await CpuStatisticsAccess.Bif(runtime);
        CpuStatisticsSample before = CpuStatisticsAccess.Direct(runtime);
        var ready = new TaskCompletionSource<CpuWorkMeasurement>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        ProcessHandle actor = runtime.Spawn(_ =>
        {
            try
            {
                CpuWorkMeasurement measured = CpuUserTimeProbe.Burn(work);
                ready.TrySetResult(measured);
                CpuStatisticsAccess.Require(release.Wait(CpuStatisticsParameters.Deadline));
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                throw;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        CpuWorkMeasurement observed = default;
        CpuStatisticsSample middle = default;
        Exception? failure = null;
        try
        {
            observed = await ready.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            // Real actor/Pump scope is STILL open on its physical worker.
            middle = CpuStatisticsAccess.Direct(runtime);
            CpuStatisticsAccess.Require(middle.Total - before.Total >= observed.Milliseconds / 2);
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
            Term reason = await actor.Completion.WaitAsync(CpuStatisticsParameters.Deadline);
            CpuStatisticsAccess.Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        CpuStatisticsSample after = CpuStatisticsAccess.Direct(runtime);
        CpuStatisticsAccess.Require(after.Total >= middle.Total);
        CpuStatisticsAccess.Require(after.Total - middle.Total < observed.Milliseconds / 2);
    }
}
