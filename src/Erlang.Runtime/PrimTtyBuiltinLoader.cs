namespace Erlang;

internal static class PrimTtyBuiltinLoader
{
    internal static async ValueTask<Term> LoadAsync(ProcessContext context, SourceLoadNifCall captured, Term[] arguments)
    {
        if (!SourceLoadNifFilename.IsValidUtf8(arguments[0]))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        SourceCodeEntry caller = captured.Consume(context);
        if (!context.Runtime.Modules.IsBootstrapTtyImage(caller.Image))
            throw new ArgumentException(PrimTtyNames.Foreign);
        try
        {
            await using NifImageLoadReservation reservation = await context.Runtime.Modules.ReserveNifImageAsync(context);
            if (!ReferenceEquals(reservation.Image, caller.Image))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
            await reservation.InitializeAndCommitAsync(cancellation =>
            {
                cancellation.ThrowIfCancellationRequested();
                if (reservation.Generation.TargetDescriptor.DeclaredNifs is not { } declarations
                    || !declarations.ToHashSet().SetEquals(PrimTtyNames.Signatures))
                    throw new ArgumentException(PrimTtyNames.Foreign);
                var owner = new PrimTtyBuiltinOwner(context.Runtime);

                return ValueTask.FromResult(new NifImageInitialized(owner, owner.Bindings()));
            });

            return PrimTtyAtoms.Ok;
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

            return Term.Tuple(PrimTtyAtoms.Error, Term.Tuple(Term.A(category), Term.String(failure.Message)));
        }
    }
}
