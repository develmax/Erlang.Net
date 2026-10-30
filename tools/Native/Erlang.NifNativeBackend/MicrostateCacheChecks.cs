using System.Runtime.CompilerServices;

namespace Erlang.NifNativeBackend;

public static class MicrostateCacheChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        var prepared = new TaskCompletionSource<MicrostateWeakVmFixture>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Exception? threadFailure = null;
        var thread = new Thread(() =>
        {
            try
            {
                MicrostateWeakVmFixture fixture = CreateDisposedFixture(work);
                // Populate the real physical-thread identity cache; it has no VM backlink.
                using (fixture.Counter.Attach(RuntimeMicrostateNames.Auxiliary, RuntimeMicrostate.Other))
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
            MicrostateWeakVmFixture fixture = await prepared.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            for (int attempt = 0; attempt < CpuStatisticsParameters.CollectionAttempts && fixture.Runtime.IsAlive; attempt++)
            {
                GC.Collect();
                await Task.Run(GC.WaitForPendingFinalizers).WaitAsync(CpuStatisticsParameters.Deadline);
                GC.Collect();
                await Task.Yield();
            }
            CpuStatisticsAccess.Require(thread.IsAlive && !fixture.Runtime.IsAlive);
            CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(MicrostateTestAccess.Frozen(fixture.Counter), MicrostateCaseNames.Aux).Sign > 0);
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
    private static MicrostateWeakVmFixture CreateDisposedFixture(TimeSpan work)
    {
        var runtime = new ProcessRuntime(maximumActorConcurrency: 1);
        RuntimeMicrostateAccounting counter = runtime.Statistics.Microstates;
        MicrostateTestAccess.Boolean(MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)).GetAwaiter().GetResult(), false);
        var weak = new WeakReference(runtime);
        try
        {
            var before = MicrostateTestAccess.Sum(MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(runtime).GetAwaiter().GetResult()), MicrostateCaseNames.Aux);
            RuntimeSchedulerAuxiliaryBatch420 batch = CpuStatisticsAccess.Dispatcher(runtime).PrepareAuxiliary420(_ =>
                CpuUserTimeProbe.Burn(work));
            // This REAL generic batch has ResourceRuntime536=null. Actor-only
            // instrumentation cannot account for its actual auxiliary interval.
            batch.Publish();
            batch.Completion.WaitAsync(CpuStatisticsParameters.Deadline).GetAwaiter().GetResult();
            CpuStatisticsAccess.Require(MicrostateTestAccess.Sum(MicrostateTestAccess.Parse(MicrostateTestAccess.Sample(runtime).GetAwaiter().GetResult()), MicrostateCaseNames.Aux) > before);
        }
        finally
        {
            runtime.DisposeAsync().AsTask().WaitAsync(CpuStatisticsParameters.Deadline).GetAwaiter().GetResult();
        }

        return new(weak, counter);
    }
}
