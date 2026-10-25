namespace Erlang.NifNativeBackend;

internal static class NifNativeAbiChecks
{
    internal static async Task Run(NifNativeCapabilityProfile profile, string probePath, string trustedProbeHash)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException(NifNativeDiagnostics.UnsupportedPlatform);
        NifNativeExportRegistration.EnsureRegistered(profile);
        using var file = NifNativeCapabilityProfile.OpenVerified(probePath, trustedProbeHash);
        nint probe = NifNativePlatformLoader.Open(file, probePath, global: false);
        try
        {
            await using var runtime = new ProcessRuntime();
            await NifNativeHarness.Context(runtime, context =>
            {
                Probe(probe, context);

                return Task.CompletedTask;
            });
        }
        finally
        {
            NifNativePlatformLoader.Close(probe);
        }
    }

    private static unsafe void Probe(nint probe, ProcessContext context)
    {
        var allocations = new NifNativeAllocationOwner();
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(allocations, failure);
        using var environment = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeNames.Module),
            0,
            failure
        );
        Term[] terms = Enumerable.Range(0, NifNativeHarnessNames.SpillCount).Select(index => (Term)Term.I(index)).ToArray();
        nuint[] handles = terms.Select(environment.Add).ToArray();
        fixed (nuint* values = handles)
        {
            var tuple = (delegate* unmanaged[Cdecl]<nint, nuint*, nuint>)NifNativePlatformLoader.Symbol(probe, NifNativeHarnessNames.TupleProbe);
            var list = (delegate* unmanaged[Cdecl]<nint, nuint*, nuint>)NifNativePlatformLoader.Symbol(probe, NifNativeHarnessNames.ListProbe);
            NifNativeHarness.Require(environment.Get(tuple(environment.Pointer, values)).Equals(Term.Tuple(terms)));
            NifNativeHarness.Require(environment.Get(list(environment.Pointer, values)).Equals(Term.List(terms)));
        }
        var emptyTuple = (delegate* unmanaged[Cdecl]<nint, nuint>)NifNativePlatformLoader.Symbol(probe, NifNativeHarnessNames.EmptyTupleProbe);
        var emptyList = (delegate* unmanaged[Cdecl]<nint, nuint>)NifNativePlatformLoader.Symbol(probe, NifNativeHarnessNames.EmptyListProbe);
        NifNativeHarness.Require(environment.Get(emptyTuple(environment.Pointer)).Equals(Term.Tuple()));
        NifNativeHarness.Require(environment.Get(emptyList(environment.Pointer)).Equals(Nil.Value));
        failure.ThrowPending();
        nuint* reasons = stackalloc nuint[3];
        reasons[0] = environment.Add(NifNativeHarnessNames.FirstReason);
        reasons[1] = environment.Add(NifNativeHarnessNames.LastReason);
        reasons[2] = environment.Add(Term.I(42));
        var pending = (delegate* unmanaged[Cdecl]<nint, nuint*, nuint>)NifNativePlatformLoader.Symbol(probe, NifNativeHarnessNames.PendingProbe);
        NifNativeHarness.Require(pending(environment.Pointer, reasons) == reasons[2]);
        NifNativeHarness.Guest(failure.ThrowPending, NifNativeHarnessNames.LastReason);
    }
}
