namespace Erlang;

internal static class PrimFileBuiltinLoader
{
    internal static async ValueTask<Term> LoadAsync(
        ProcessContext context,
        SourceLoadNifCall captured,
        Term[] arguments
    )
    {
        if (!SourceLoadNifFilename.IsValidUtf8(arguments[0]))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        SourceCodeEntry caller = captured.Consume(context);
        if (arguments[1] is not Pid janitor || !context.Runtime.IsCurrentLocal(janitor))
            throw new ArgumentException(PrimFileBuiltinNames.InvalidOwner);
        try
        {
            await using NifImageLoadReservation reservation = await context.Runtime.Modules.ReserveNifImageAsync(context);
            if (!ReferenceEquals(reservation.Image, caller.Image))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
            await reservation.InitializeAndCommitAsync(cancellation =>
            {
                cancellation.ThrowIfCancellationRequested();
                if (reservation.Generation.TargetDescriptor.DeclaredNifs is not { } declarations
                    || PrimFileBuiltinNames.Signatures.Any(signature => !declarations.Contains(signature)))
                    throw new NotSupportedException(PrimFileBuiltinNames.MissingDeclaration);
                var owner = new PrimFileBuiltinOwner(context.Runtime);

                return ValueTask.FromResult(new NifImageInitialized(owner, owner.Bindings()));
            });

            return Term.A(CommonAtomNames.Ok);
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

            return Term.Tuple(Term.A(CommonAtomNames.Error), Term.Tuple(Term.A(category), Term.String(failure.Message)));
        }
    }
}
