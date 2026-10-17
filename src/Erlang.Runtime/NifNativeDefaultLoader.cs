namespace Erlang;

internal static class NifNativeDefaultLoader
{
    internal static async ValueTask<Term> LoadAsync(
        ProcessContext context,
        SourceLoadNifCall captured,
        Term[] arguments,
        Func<string, NifNativeReviewedBasicAbi?> approve
    )
    {
        string path = NifNativeFilename.Resolve(arguments[0]);
        SourceCodeEntry caller = captured.Consume(context);
        NifNativeReviewedBasicAbi capability = approve(path)
            ?? throw new UnauthorizedAccessException(NifNativeDefaultLoaderNames.Permission);
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(capability.LibraryPath, path, comparison))
            throw new UnauthorizedAccessException(NifNativeDefaultLoaderNames.Permission);
        var profile = NifNativeCapabilityProfile.ForReviewedBasicAbi(capability);
        try
        {
            await using var reservation = await context.Runtime.Modules.ReserveNifImageAsync(context);
            if (!ReferenceEquals(reservation.Image, caller.Image))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
            await reservation.InitializeAndCommitAsync(cancellation => NifNativeImageInitializer.InitializeAsync(
                reservation,
                profile,
                arguments[1],
                cancellation
            ));

            return Term.A(CommonAtomNames.Ok);
        }
        catch (NifNativeLoadStageException failure)
        {
            return Error(failure.Category, failure.Message);
        }
        catch (NifImageAdmissionException failure) when (failure.Failure is
            NifImageAdmissionFailure.OldCode or NifImageAdmissionFailure.Reload or NifImageAdmissionFailure.UpgradeUnsupported)
        {
            string category = failure.Failure switch
            {
                NifImageAdmissionFailure.OldCode => NifNativeDefaultLoaderNames.OldCode,
                NifImageAdmissionFailure.Reload => NifNativeDefaultLoaderNames.Reload,
                _ => NifNativeDefaultLoaderNames.Upgrade
            };

            return Error(category, failure.Message);
        }
        catch (NotSupportedException failure) when (failure.Message is
            NifNativeDiagnostics.UnsupportedNativeEntry or NifNativeDiagnostics.NoDeclaredNif or
            NifNativeDiagnostics.UntrustedFunction or NifNativeDiagnostics.ModuleIdentity)
        {
            return Error(NifNativeDefaultLoaderNames.BadLibrary, failure.Message);
        }
        catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.NullNativeEntry)
        {
            return Error(NifNativeDefaultLoaderNames.BadLibrary, failure.Message);
        }
        catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.LoadVeto)
        {
            return Error(NifNativeDefaultLoaderNames.Load, failure.Message);
        }
    }

    private static Term Error(string category, string reason) => Term.Tuple(
        Term.A(CommonAtomNames.Error),
        Term.Tuple(Term.A(category), Term.String(reason))
    );
}
