namespace Erlang;

internal static class BeamLoaderApi
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

    private static Term PrepareImage(ProcessContext context, Term module, Term bytes)
    {
        if (module is not Atom name || bytes is not BitString { IsBinary: true } binary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] input = binary.ToArray();
        try
        {
            BeamStructure structure = BeamStructureReader.Read(input, context.Runtime.Modules.PreparationDecodeOptions());
            if (structure.Atoms[1] != name.Name)
                return Error(BeamLoaderNames.BadFile);
            PreparedBeamImage image = context.Runtime.Modules.PrepareBeam(input);

            return context.Runtime.ReferenceResources.CreatePrepared(image);
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
        if (!context.Runtime.ReferenceResources.TryReadPreparedMetadata(identity, out PreparedBeamImage? image))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.A(image!.HasOnLoad ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
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

    internal static Term LoadModule(ProcessContext context, Term module, Term bytes)
    {
        try
        {
            return LoadImage(context, module, bytes);
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw WithErrorInfo(
                context,
                exception,
                BeamLoaderNames.LoadModule,
                [module, bytes]
            );
        }
    }

    private static Term LoadImage(ProcessContext context, Term module, Term bytes)
    {
        Term prepared = Prepare(context, module, bytes);
        if (prepared is not ReferenceTerm)
            return prepared;
        Term result = Finish(context, Cons.From([prepared]));
        if (result is Atom { Name: BeamLoaderNames.Ok })
            return Term.Tuple(Term.A(BeamLoaderNames.Module), module);
        if (result is TupleTerm failure)
            return Term.Tuple(Term.A(BeamLoaderNames.Error), failure.Items[0]);

        throw new InvalidOperationException(BeamLoaderDiagnostics.UnexpectedLoadingResult);
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
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), info, function)
        };
    }

    private static Term Error(string reason)
        => Term.Tuple(Term.A(BeamLoaderNames.Error), Term.A(reason));

}
