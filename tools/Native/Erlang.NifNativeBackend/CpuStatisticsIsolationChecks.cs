namespace Erlang.NifNativeBackend;

public static class CpuStatisticsIsolationChecks
{
    public static async Task Run()
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        _ = await CpuStatisticsAccess.Bif(first);
        _ = await CpuStatisticsAccess.Bif(second);
        _ = await CpuStatisticsAccess.Bif(first);
        _ = await CpuStatisticsAccess.Bif(second);
        CpuStatisticsSample firstBefore = await CpuStatisticsAccess.Bif(first);
        CpuStatisticsSample secondBefore = await CpuStatisticsAccess.Bif(second);
        CpuWorkMeasurement firstWork = await CpuStatisticsAccess.Actor(first,
            _ => ValueTask.FromResult(CpuUserTimeProbe.Burn(work)));
        CpuStatisticsSample firstAfter = await CpuStatisticsAccess.Bif(first);
        CpuStatisticsSample secondAfter = await CpuStatisticsAccess.Bif(second);
        CpuStatisticsAccess.Require(firstAfter.Total - firstBefore.Total >= firstWork.Milliseconds / 2);
        CpuStatisticsAccess.Require(secondAfter.Total - secondBefore.Total < firstWork.Milliseconds / 3);

        CpuWorkMeasurement secondWork = await CpuStatisticsAccess.Actor(second,
            _ => ValueTask.FromResult(CpuUserTimeProbe.Burn(work)));
        CpuStatisticsSample secondFinal = await CpuStatisticsAccess.Bif(second);
        CpuStatisticsSample firstFinal = await CpuStatisticsAccess.Bif(first);
        CpuStatisticsAccess.Require(secondFinal.Total - secondAfter.Total >= secondWork.Milliseconds / 2);
        CpuStatisticsAccess.Require(firstFinal.Total - firstAfter.Total < secondWork.Milliseconds / 3);

        CpuWorkMeasurement hostWork = await CpuHostUserWork.Burn(work);
        CpuStatisticsSample hostFirst = await CpuStatisticsAccess.Bif(first);
        CpuStatisticsSample hostSecond = await CpuStatisticsAccess.Bif(second);
        CpuStatisticsAccess.Require(hostFirst.Total - firstFinal.Total < hostWork.Milliseconds / 3);
        CpuStatisticsAccess.Require(hostSecond.Total - secondFinal.Total < hostWork.Milliseconds / 3);
    }
}
