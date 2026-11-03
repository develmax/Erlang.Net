using System.Runtime.CompilerServices;

namespace Erlang.NifNativeBackend;

public static class CpuStatisticsAuxiliaryChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        var prepared = new TaskCompletionSource<CpuVmLeakFixture>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Exception? threadFailure = null;
        var thread = new Thread(() =>
        {
            try
            {
                CpuVmLeakFixture fixture = CreateDisposedFixture(work);
                // Also populate this retained physical thread's reader cache
                // using the detached counter; there is no VM backlink.
                using (fixture.Counter.Enter())
                    _ = CpuUserTimeProbe.Burn(work);
                prepared.TrySetResult(fixture);
                CpuStatisticsAccess.Require(release.Wait(CpuStatisticsParameters.Deadline));
                GC.KeepAlive(fixture.Counter);
            }
            catch (Exception error)
            {
                threadFailure = error;
                prepared.TrySetException(error);
            }
        })
        { IsBackground = true };
        thread.Start();
        Exception? failure = null;
        try
        {
            CpuVmLeakFixture fixture = await prepared.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            for (int attempt = 0; attempt < CpuStatisticsParameters.CollectionAttempts && fixture.Runtime.IsAlive; attempt++)
            {
                GC.Collect();
                await Task.Run(GC.WaitForPendingFinalizers).WaitAsync(CpuStatisticsParameters.Deadline);
                GC.Collect();
                await Task.Yield();
            }
            CpuStatisticsAccess.Require(thread.IsAlive && !fixture.Runtime.IsAlive);
            CpuStatisticsAccess.Require(fixture.Counter.SampleMilliseconds() > 0);
            GC.KeepAlive(fixture.Counter);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            release.Set();
            if (!thread.Join(CpuStatisticsParameters.Deadline))
                failure = failure is null ? new TimeoutException(CpuStatisticsParameters.Failure)
                    : new AggregateException(failure, new TimeoutException(CpuStatisticsParameters.Failure));
        }
        if (threadFailure is not null) failure = failure is null ? threadFailure : new AggregateException(failure, threadFailure);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CpuVmLeakFixture CreateDisposedFixture(TimeSpan work)
    {
        var runtime = new ProcessRuntime(maximumActorConcurrency: 1);
        RuntimeUserCpuAccounting counter = runtime.Statistics.UserCpu;
        var weak = new WeakReference(runtime);
        try
        {
            var before = counter.SampleMilliseconds();
            CpuWorkMeasurement measured = default;
            RuntimeSchedulerAuxiliaryBatch420 batch = CpuStatisticsAccess.Dispatcher(runtime).PrepareAuxiliary420(_ =>
                measured = CpuUserTimeProbe.Burn(work));
            // This REAL generic batch has ResourceRuntime536=null. Actor-only
            // instrumentation cannot account for its actual CPU work.
            batch.Publish();
            batch.Completion.WaitAsync(CpuStatisticsParameters.Deadline).GetAwaiter().GetResult();
            CpuStatisticsAccess.Require(counter.SampleMilliseconds() - before >= measured.Milliseconds / 2);
        }
        finally
        {
            runtime.DisposeAsync().AsTask().WaitAsync(CpuStatisticsParameters.Deadline).GetAwaiter().GetResult();
        }

        return new(weak, counter);
    }
}
