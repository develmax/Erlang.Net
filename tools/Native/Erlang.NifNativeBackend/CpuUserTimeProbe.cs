using System.Diagnostics;

namespace Erlang.NifNativeBackend;

internal static class CpuUserTimeProbe
{
    internal static CpuWorkMeasurement Burn(TimeSpan targetCpu)
    {
        RuntimeThreadUserCpuReader reader;
        try
        {
            reader = RuntimeThreadUserCpuReader.OpenCurrent(RuntimeOsThreadIdentity.Current());
        }
        catch (Exception error)
        {
            throw new CpuMeasurementUnavailableException(error);
        }
        try
        {
            long first = reader.ReadTicks(), previous = first, step = 0;
            long wall = Stopwatch.GetTimestamp();
            while (previous - first < targetCpu.Ticks)
            {
                Thread.SpinWait(CpuStatisticsParameters.SpinIterations);
                long next = reader.ReadTicks();
                if (next < previous) throw new CpuMeasurementUnavailableException();
                step = Math.Max(step, next - previous);
                previous = next;
                if (Stopwatch.GetElapsedTime(wall) > CpuStatisticsParameters.Deadline)
                    throw new CpuMeasurementUnavailableException();
            }
            if (step == 0) throw new CpuMeasurementUnavailableException();

            return new(previous - first, step);
        }
        finally
        {
            reader.Dispose();
        }
    }

    internal static TimeSpan CalibrateWork()
    {
        CpuWorkMeasurement measurement = Burn(CpuStatisticsParameters.CalibrationCpu);
        long ticks = Math.Max(
            CpuStatisticsParameters.MinimumWorkCpu.Ticks,
            checked(measurement.LargestObservedStepTicks * CpuStatisticsParameters.AccountingQuantaPerWork)
        );
        if (ticks > CpuStatisticsParameters.MaximumWorkCpu.Ticks)
            throw new CpuMeasurementUnavailableException();

        return TimeSpan.FromTicks(ticks);
    }
}
