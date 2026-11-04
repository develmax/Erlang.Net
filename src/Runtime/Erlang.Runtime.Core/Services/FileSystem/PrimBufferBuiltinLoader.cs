namespace Erlang;

internal static class PrimBufferBuiltinLoader
{
    internal static async ValueTask<Term> LoadAsync(ProcessContext context, SourceLoadNifCall captured, Term[] arguments)
    {
        if (!SourceLoadNifFilename.IsValidUtf8(arguments[0]) || arguments[1] is not Integer number || number.Value != 0)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        SourceCodeEntry caller = captured.Consume(context);
        await using NifImageLoadReservation reservation = await context.Runtime.Modules.ReserveNifImageAsync(context);
        if (!ReferenceEquals(reservation.Image, caller.Image))
        {
            throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
        }
        await reservation.InitializeAndCommitAsync(cancellation =>
        {
            cancellation.ThrowIfCancellationRequested();
            if (reservation.Generation.TargetDescriptor.DeclaredNifs is not { } declarations
                || PrimBufferBuiltinNames.Signatures.Any(signature => !declarations.Contains(signature)))
            {
                throw new NotSupportedException(PrimFileBuiltinNames.MissingDeclaration);
            }
            var owner = new PrimBufferBuiltinOwner(context.Runtime);

            return ValueTask.FromResult(new NifImageInitialized(owner, owner.Bindings()));
        });

        return Term.A(CommonAtomNames.Ok);
    }
}
