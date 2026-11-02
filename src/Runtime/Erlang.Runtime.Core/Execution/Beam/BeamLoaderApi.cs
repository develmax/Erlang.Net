namespace Erlang;

internal static partial class BeamLoaderApi
{
    internal static Term Prepare(ProcessContext context, Term module, Term bytes)
    {
        Term code = Decompress(bytes);
        try
        {
            return PrepareImage(context, module, code);
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

    private static Term Decompress(Term bytes)
    {
        if (bytes is BitString { IsBinary: true } binary && binary.ToArray().AsSpan().StartsWith(BeamIffWire.FormId))
            return bytes;
        try
        {
            byte[] input = ErlangIoData.Flatten(bytes, false, false).ToArray();

            return new BitString(BeamGzipMembers.Inflate(input));
        }
        catch (Exception exception) when (exception is ErlangException or InvalidDataException or IOException or OverflowException or ArgumentException)
        {
            return bytes;
        }
    }

    internal static Term PrepareImage(ProcessContext context, Term module, Term bytes)
        => PrepareImageCore807(
            context,
            module,
            bytes,
            false,
            out _,
            out _
        );

    private static Term PreparePrivate807(
        ProcessContext context,
        Term module,
        Term bytes,
        out PreparedBeamImage? image,
        out PreparedLoaderOwner807? owner
    )
    {
        Term code = Decompress(bytes);
        try
        {
            return PrepareImageCore807(
                context,
                module,
                code,
                true,
                out image,
                out owner
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

    private static Term PrepareImageCore807(
        ProcessContext context,
        Term module,
        Term bytes,
        bool privateHandle,
        out PreparedBeamImage? preparedImage,
        out PreparedLoaderOwner807? privateOwner
    )
    {
        preparedImage = null;
        privateOwner = null;
        if (module is not Atom name || bytes is not BitString { IsBinary: true } binary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] input = binary.ToArray();
        try
        {
            BeamStructure structure = BeamStructureReader.Read(input, context.Runtime.Modules.PreparationDecodeOptions());
            if (structure.Atoms[1] != name.Name)
                return Error(BeamLoaderNames.BadFile);
            PreparedBeamImage? caller = context.CaptureBeamLoaderSource();
            PreparedBeamImage image = caller is not null
                ? context.Runtime.Modules.PrepareBeam(input,
                    new BeamDependencySelection(caller.Code.RegistryModuleDependencies.Append(name.Name)))
                : context.Runtime.Modules.PrepareBeam(input);
            preparedImage = image;
            try
            {
                return privateHandle
                    ? context.Runtime.ReferenceResources.CreatePrivatePrepared807(image, out privateOwner)
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

    internal static Term HasOnLoad(ProcessContext context, Term reference)
    {
        if (reference is not ReferenceTerm identity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool value = context.Runtime.ReferenceResources.ReadPreparedOnLoad807(identity);

        return Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
    }

    internal static ValueTask<Term> FinishAsync807(ProcessContext context, Term list)
    {
        context.Runtime.Modules.RequirePreparedFinishCaller807(context);
        PreparedLoaderFinishPlan807 plan = PreparedLoaderFinishPlan807.Capture807(context.Runtime.ReferenceResources, list);

        return plan.Run807(context);
    }

    internal static Term Finish(ProcessContext context, Term list)
    {
        Term[] references = Cons.Items(list).ToArray();
        var identities = new List<ReferenceTerm>();
        foreach (Term term in references)
        {
            if (term is not ReferenceTerm identity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            identities.Add(identity);
        }

        return context.Runtime.ReferenceResources.FinishPreparedReferences(identities);
    }

    internal static async ValueTask<Term> LoadModuleAsync(ProcessContext context, Term module, Term bytes)
    {
        var preparation371 = new PreparedImageAdmission371();
        ResourceReferenceTerm? privateHandle807 = null;
        Exception? primary807 = null;
        try
        {
            Term prepared = await PreparePrivateAsync371(
                context,
                module,
                bytes,
                preparation371
            );
            if (prepared is not ResourceReferenceTerm reference) return prepared;
            privateHandle807 = reference;
            Term result = await FinishAsync807(context, Cons.From([prepared]));
            if (result is Atom { Name: BeamLoaderNames.Ok })
                return Term.Tuple(Term.A(BeamLoaderNames.Module), module);
            if (result is TupleTerm failure)
                return Term.Tuple(Term.A(BeamLoaderNames.Error), failure.Items[0]);

            throw new InvalidOperationException(BeamLoaderDiagnostics.UnexpectedLoadingResult);
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Error)
        {
            primary807 = WithErrorInfo(
                context,
                exception,
                BeamLoaderNames.LoadModule,
                [module, bytes]
            );
            throw primary807;
        }
        catch (ModuleSourceMutationAdmissionException failure) when (failure.Failure == ModuleSourceMutationFailure.RuntimeClosing
            && ReferenceEquals(failure.Registry, context.Runtime.Modules))
        {
            primary807 = new ModuleSourceMutationClosedException(context.Runtime.Modules, context.Cancellation);
            throw primary807;
        }
        catch (Exception error)
        {
            primary807 = error;
            throw;
        }
        finally
        {
            await PrivatePreparedLoaderScope807.CloseAsync(
                preparation371.Image,
                privateHandle807,
                preparation371.Owner,
                primary807
            );
        }
    }

    internal static Term DynamicCallOnLoad(ProcessContext context, Term module)
        => throw new ErlangException(ErlangErrorReasons.BadArgument);

    internal static ValueTask<Term> FinishOnLoadAsync(ProcessContext context, Term module, Term keep)
    {
        bool retain = CoreModules.Bool(keep);

        return retain
            ? context.Runtime.Modules.ModifySourceCodeAsync(context, () => FinishOnLoad(context, module, keep))
            : context.Runtime.Modules.ModifyCodeAsync(context, () => FinishOnLoad(context, module, keep));
    }

    internal static Term FinishOnLoad(ProcessContext context, Term module, Term keep)
    {
        bool retain = CoreModules.Bool(keep);
        if (module is not Atom name)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return context.Runtime.Modules.FinishLoaderOnLoad(name.Name, retain);
    }

    private static ErlangException WithErrorInfo(
        ProcessContext context,
        ErlangException exception,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        var info = new MapTerm([
            new KeyValuePair<Term, Term>(Term.A(BeamLoaderNames.ErrorInfoModule), Term.A(BeamLoaderNames.ErrorFormatter))
        ]);

        return new ErlangException(exception.Reason, exception.ExceptionClass)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(
                Cons.From(arguments),
                info,
                function,
                function == BeamLoaderNames.LoadModule ? context.Runtime.NativeLoadModuleErrorLocation527 : null
            )
        };
    }

    private static Term Error(string reason)
        => Term.Tuple(Term.A(BeamLoaderNames.Error), Term.A(reason));

}
