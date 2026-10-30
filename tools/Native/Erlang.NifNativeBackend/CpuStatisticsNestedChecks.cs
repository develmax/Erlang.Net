namespace Erlang.NifNativeBackend;

public static class CpuStatisticsNestedChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        RuntimeUserCpuAccounting firstCounter = first.Statistics.UserCpu;
        RuntimeUserCpuAccounting secondCounter = second.Statistics.UserCpu;
        var marker = new InvalidOperationException(CpuStatisticsParameters.Failure);
        using (firstCounter.Enter())
        {
            CpuWorkMeasurement firstWork = CpuUserTimeProbe.Burn(work);
            var firstBeforeSecond = firstCounter.SampleMilliseconds();
            var secondBefore = secondCounter.SampleMilliseconds();
            CpuWorkMeasurement secondWork = default;
            try
            {
                using (secondCounter.Enter())
                {
                    secondWork = CpuUserTimeProbe.Burn(work);
                    CpuStatisticsAccess.Require(secondCounter.SampleMilliseconds() - secondBefore >= secondWork.Milliseconds / 2);
                    CpuStatisticsAccess.Require(firstCounter.SampleMilliseconds() - firstBeforeSecond < firstWork.Milliseconds / 3);
                    throw marker;
                }
            }
            catch (InvalidOperationException error) when (ReferenceEquals(error, marker)) { }
            var secondAfter = secondCounter.SampleMilliseconds();
            using (firstCounter.Enter())
            {
                CpuWorkMeasurement resumed = CpuUserTimeProbe.Burn(work);
                CpuStatisticsAccess.Require(firstCounter.SampleMilliseconds() - firstBeforeSecond >= resumed.Milliseconds / 2);
            }
            CpuStatisticsAccess.Require(secondCounter.SampleMilliseconds() == secondAfter);
        }
    }
}
