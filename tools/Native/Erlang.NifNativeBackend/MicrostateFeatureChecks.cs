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
    }
}
