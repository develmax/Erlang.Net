namespace Erlang.NifNativeBackend;

public static class MicrostateSourceTailChecks
{
    public static async Task SuccessfulPhases()
    {
        _ = await MicrostateSourceTailLifecycleChecks.Run(MicrostateSourceTailCaseNames.SuccessfulPhases);
    }

    public static async Task WithheldClose()
    {
        _ = await MicrostateSourceTailLifecycleChecks.Run(MicrostateSourceTailCaseNames.WithheldClose);
    }

    public static async Task RunningClose()
    {
        _ = await MicrostateSourceTailLifecycleChecks.Run(MicrostateSourceTailCaseNames.RunningClose);
    }

    public static async Task FirstFailure()
    {
        _ = await MicrostateSourceTailLifecycleChecks.Run(MicrostateSourceTailCaseNames.FirstFailure);
    }

    public static async Task FinishFailure()
    {
        _ = await MicrostateSourceTailLifecycleChecks.Run(MicrostateSourceTailCaseNames.FinishFailure);
    }
}
