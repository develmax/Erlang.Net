namespace Erlang;

internal static partial class BeamLoaderApi
{
    // Explicit per-operation result carrier also preserves partially assigned
    // private owners for the existing LoadModule finally cleanup on failure.
    private sealed class PreparedImageAdmission371
    {
        internal PreparedBeamImage? Image;
        internal PreparedLoaderOwner807? Owner;
    }

    internal static async ValueTask<Term> PrepareAsync371(ProcessContext context, Term module, Term bytes)
    {
        Term code = Decompress(bytes);
        try
        {
            return await PrepareImageAsync371(
                context,
                module,
                code,
                false,
                new PreparedImageAdmission371()
            );
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw WithErrorInfo(
                context,
                exception,
                BeamLoaderNames.Prepare,
                [module, code]
            );
        }
    }

    private static async ValueTask<Term> PreparePrivateAsync371(
        ProcessContext context,
        Term module,
        Term bytes,
        PreparedImageAdmission371 admission
    )
    {
        Term code = Decompress(bytes);
        try
        {
            return await PrepareImageAsync371(
                context,
                module,
                code,
                true,
                admission
            );
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw WithErrorInfo(
                context,
                exception,
                BeamLoaderNames.Prepare,
                [module, code]
            );
        }
    }

    private static async ValueTask<Term> PrepareImageAsync371(
        ProcessContext context,
        Term module,
        Term bytes,
        bool privateHandle,
        PreparedImageAdmission371 admission
    )
    {
        if (module is not Atom name || bytes is not BitString { IsBinary: true } binary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] input = binary.ToArray();
        try
        {
            BeamStructure structure = BeamStructureReader.Read(input, context.Runtime.Modules.PreparationDecodeOptions());
            if (structure.Atoms[1] != name.Name)
                return Error(BeamLoaderNames.BadFile);
            PreparedBeamImage? caller = context.CaptureBeamLoaderSource();
            BeamDependencySelection? dependencies = caller is null ? null
                : new BeamDependencySelection(caller.Code.RegistryModuleDependencies.Append(name.Name));
            // Input validation, decompression, name and dependency snapshot are
            // complete BEFORE the existing source-admission semaphore can wait.
            PreparedBeamImage image = await context.Runtime.Modules.PrepareBeamForActor371(context, input, dependencies);
            admission.Image = image;
            try
            {
                return privateHandle
                    ? context.Runtime.ReferenceResources.CreatePrivatePrepared807(image, out admission.Owner)
                    : context.Runtime.ReferenceResources.CreatePrepared(image);
            }
            catch (Exception primary)
            {
                try
                {
                    image.Dispose();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }
        catch (InvalidDataException)
        {
            return Error(BeamLoaderNames.BadFile);
        }
    }
}
