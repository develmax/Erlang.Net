namespace Erlang.Tests;

internal static class BeamLoaderMfaChecks
{
    internal static bool Owns(string module, string function, int arity)
        => module == ErlangModuleNames.Module && (function, arity) is
            (BeamLoaderAdmissionNames.Prepare, 2) or
            (BeamLoaderAdmissionNames.Query, 1) or
            (BeamLoaderAdmissionNames.Finish, 1) or
            (BeamLoaderDirectNames.Callback, 1) or
            (BeamLoaderAdmissionNames.FinishOnLoad, 2) or
            (BeamLoaderLifecycleNames.Load, 2);

    internal static Task Run(BeamLoaderMfaCase test)
        => BeamLoaderLifecycleChecks.RunCase(test);

    internal static Task Admission() => BeamLoaderAdmissionChecks.Run();

    internal static Task Races() => BeamLoaderAdmissionRaceChecks.Run(BeamLoaderLifecycleChecks.ConstantBytes());

    internal static Task Padded() => BeamLoaderPaddedReferenceChecks.Run(BeamLoaderLifecycleChecks.ConstantBytes());

    internal static async Task Direct()
    {
        await BeamLoaderDirectChecks.Run();
        await BeamLoaderDirectChecks.RunNative();
    }
}
