using System.Diagnostics;

namespace Erlang.NifNativeBackend;

public static class CpuStatisticsDirtyChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 2);
        _ = await CpuStatisticsAccess.Bif(runtime);
        _ = await Schedule(runtime, DirtyNativeKind419.Cpu, (_, _) => Nil.Value);
        _ = await Schedule(runtime, DirtyNativeKind419.Io, (_, _) => Nil.Value);
        CpuStatisticsSample beforeBurn = CpuStatisticsAccess.Direct(runtime);
        CpuWorkMeasurement measured = default;
        _ = await Schedule(
            runtime,
            DirtyNativeKind419.Cpu,
            (context, _) =>
        {
            CpuStatisticsAccess.Require(DirtyNativeThread419.Read(context) == DirtyNativeKind419.Cpu);
            measured = CpuUserTimeProbe.Burn(work);

            return Nil.Value;
        }
        );
        CpuStatisticsSample afterBurn = CpuStatisticsAccess.Direct(runtime);
        CpuStatisticsAccess.Require(afterBurn.Total - beforeBurn.Total >= measured.Milliseconds / 2);

        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        long sleeperTicks = 0;
        TimeSpan sleeperWall = default;
        Task<Term> pending = Schedule(
            runtime,
            DirtyNativeKind419.Io,
            (context, _) =>
        {
            CpuStatisticsAccess.Require(DirtyNativeThread419.Read(context) == DirtyNativeKind419.Io);
            RuntimeThreadUserCpuReader reader = RuntimeThreadUserCpuReader.OpenCurrent(RuntimeOsThreadIdentity.Current());
            try
            {
                long first = reader.ReadTicks();
                long wall = Stopwatch.GetTimestamp();
                entered.TrySetResult();
                CpuStatisticsAccess.Require(release.Wait(CpuStatisticsParameters.Deadline));
                sleeperTicks = reader.ReadTicks() - first;
                sleeperWall = Stopwatch.GetElapsedTime(wall);

                return Nil.Value;
            }
            finally
            {
                reader.Dispose();
            }
        }
        );
        CpuWorkMeasurement host = default;
        CpuStatisticsSample beforeSleep = default, duringSleep = default;
        Exception? failure = null;
        try
        {
            Task first = await Task.WhenAny(entered.Task, pending).WaitAsync(CpuStatisticsParameters.Deadline);
            if (ReferenceEquals(first, pending)) _ = await pending;
            await entered.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            beforeSleep = CpuStatisticsAccess.Direct(runtime);
            // Measured host USER CPU establishes the blocked interval. No fixed
            // wall-time sleep is used as a fake CPU measurement.
            host = await CpuHostUserWork.Burn(work);
            duringSleep = CpuStatisticsAccess.Direct(runtime);
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
            _ = await pending.WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        CpuStatisticsAccess.Require(duringSleep.Total - beforeSleep.Total < host.Milliseconds / 3);
        CpuStatisticsAccess.Require(sleeperTicks / TimeSpan.TicksPerMillisecond < host.Milliseconds / 3);
        CpuStatisticsAccess.Require(sleeperWall.Ticks >= host.UserTicks / 2);
    }

    private static async Task<Term> Schedule(
        ProcessRuntime runtime,
        DirtyNativeKind419 kind,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body
    )
    {
        var physical = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term? result = null;
        Exception? failure = null;
        try
        {
            result = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context => await runtime.ScheduleDirtyNative419(
                    context,
                    kind,
                    CpuStatisticsParameters.DirtyModule,
                    CpuStatisticsParameters.DirtyFunction,
                    Array.Empty<Term>(),
                    body,
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
            {
                Task actualCompletion = await physical.Task.WaitAsync(CpuStatisticsParameters.Deadline);
                await actualCompletion.WaitAsync(CpuStatisticsParameters.Deadline);
            }
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();

        return result!;
    }
}
