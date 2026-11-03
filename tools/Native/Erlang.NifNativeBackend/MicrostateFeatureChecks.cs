namespace Erlang.NifNativeBackend;

public static class MicrostateFeatureChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), false);
        // The setter actor has actually exited; enabled state is VM-global, not caller-owned.
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), true);
        MicrostateSample[] enabled = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        CpuStatisticsAccess.Require(enabled.Length > 0);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, Term.A(MicrostateCaseNames.Reset)), true);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(false)), true);
        MicrostateSample[] first = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        _ = await CpuHostUserWork.Burn(CpuUserTimeProbe.CalibrateWork());
        MicrostateSample[] second = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime));
        foreach (MicrostateSample old in first)
        {
            MicrostateSample current = second.Single(value => value.Type == old.Type && value.Identity == old.Identity);
            foreach (string state in MicrostateCaseNames.Keys)
                CpuStatisticsAccess.Require(current.Counters[state] == old.Counters[state]);
        }
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(false)), false);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, Term.A(MicrostateCaseNames.Reset)), false);
        CpuStatisticsAccess.Require(MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime))
            .All(value => value.Counters.Values.All(counter => counter.IsZero)));
        await LateAuxiliary989(runtime);
    }

    private static async Task LateAuxiliary989(ProcessRuntime runtime)
    {
        CpuStatisticsAccess.Require(!runtime.HasNativeResources524);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), false);
        NativeResourceDomain524 auxiliary = runtime.NativeResources524;
        await auxiliary.Ready989.WaitAsync(CpuStatisticsParameters.Deadline);
        MicrostateSample first = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime))
            .Single(value => value.Type == MicrostateCaseNames.Aux);
        CpuStatisticsAccess.Require(first.Counters.Values.All(value => value.IsZero));
        // Pinned late managed registration starts perf=0/TSD none. A repeated
        // normal enable is a no-op, not permission to seed another thread.
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), true);
        MicrostateSample noOp = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime))
            .Single(value => value.Type == MicrostateCaseNames.Aux);
        CpuStatisticsAccess.Require(noOp.Identity == first.Identity && noOp.Counters.Values.All(value => value.IsZero));
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(false)), true);
        MicrostateTestAccess.Boolean(await MicrostateTestAccess.Set(runtime, CoreModules.Boolean(true)), false);
        _ = await CpuHostUserWork.Burn(CpuUserTimeProbe.CalibrateWork());
        MicrostateSample attached = MicrostateTestAccess.Parse(await MicrostateTestAccess.Sample(runtime))
            .Single(value => value.Type == MicrostateCaseNames.Aux);
        CpuStatisticsAccess.Require(attached.Identity == first.Identity && attached.Counters.Values.Any(value => value > 0));
    }

}
