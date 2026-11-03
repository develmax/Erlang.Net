namespace Erlang.NifNativeBackend;

public static class SchedulerWallTimeConcurrentChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 4);
        await using var first = new SchedulerWallTimeTestActor(runtime);
        await using var second = new SchedulerWallTimeTestActor(runtime);
        await using var observer = new SchedulerWallTimeTestActor(runtime);
        Term[] enabled = await Task.WhenAll(first.Flag(true), second.Flag(true));
        CpuStatisticsAccess.Require(enabled.Count(item => item.Equals(CoreModules.Boolean(false))) == 1
            && enabled.Count(item => item.Equals(CoreModules.Boolean(true))) == 1);
        CpuStatisticsAccess.Require(SchedulerWallTimeSamples.Read(await observer.Sample()).Count > 0);
        Term[] disabled = await Task.WhenAll(first.Flag(false), second.Flag(false));
        foreach (Term result in disabled) SchedulerWallTimeSamples.Boolean(result, true);
        SchedulerWallTimeSamples.Undefined(await observer.Sample());
    }
}
