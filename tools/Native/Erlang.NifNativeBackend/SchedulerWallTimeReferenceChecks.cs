namespace Erlang.NifNativeBackend;

public static class SchedulerWallTimeReferenceChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 4);
        await using var first = new SchedulerWallTimeTestActor(runtime);
        await using var second = new SchedulerWallTimeTestActor(runtime);
        await using var unrelated = new SchedulerWallTimeTestActor(runtime);
        SchedulerWallTimeSamples.Undefined(await unrelated.Sample());
        SchedulerWallTimeSamples.Boolean(await first.Flag(true), false);
        SchedulerWallTimeSamples.Boolean(await first.Flag(true), true);
        SchedulerWallTimeSamples.Boolean(await first.Flag(false), true);
        CpuStatisticsAccess.Require(SchedulerWallTimeSamples.Read(await unrelated.Sample()).Count > 0);
        SchedulerWallTimeSamples.Boolean(await second.Flag(true), true);
        SchedulerWallTimeSamples.Boolean(await unrelated.Flag(false), true);
        SchedulerWallTimeSamples.Boolean(await first.Flag(false), true);
        CpuStatisticsAccess.Require(SchedulerWallTimeSamples.Read(await unrelated.Sample()).Count > 0);
        await second.DisposeAsync();
        SchedulerWallTimeSamples.Undefined(await unrelated.Sample());
        SchedulerWallTimeSamples.Boolean(await unrelated.Flag(false), false);
        SchedulerWallTimeSamples.Boolean(await first.Flag(true), false);
        SchedulerWallTimeSamples.Boolean(await first.Flag(true), true);
        await first.DisposeAsync();
        SchedulerWallTimeSamples.Undefined(await unrelated.Sample());
    }
}
