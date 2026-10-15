namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly Dictionary<string, BeamOnLoadTicket> loaderPendingImages = new(StringComparer.Ordinal);

    internal Term FinishLoaderReferences(RuntimeReferenceResources resources, IReadOnlyList<ReferenceTerm> references)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        lock (gate)
            return resources.AdmitPreparedReferences(references);
    }

    internal Term FinishLoaderImages(PreparedBeamImage[] images)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            ThrowIfSourceClosing();
            foreach (PreparedBeamImage image in images)
                if (!ReferenceEquals(image.Owner, this) || image.Consumed)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (images.Length > 1 && images.Any(image => image.HasOnLoad))
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            if (images.Length == 1 && images[0].HasOnLoad)
                return StageLoaderOnLoad(images[0]);
            BeamPublicationResult result = PublishPreparedBeam(images);

            return result.Status switch
            {
                BeamPublicationStatus.Published => Term.A(BeamLoaderNames.Ok),
                BeamPublicationStatus.Duplicated => FailureList(BeamLoaderNames.Duplicated, result.Modules),
                BeamPublicationStatus.NotPurged => FailureList(BeamLoaderNames.NotPurged, result.Modules),
                _ => throw new InvalidOperationException(BeamLoaderDiagnostics.UnexpectedPublication)
            };
        }
    }

    private static Term FailureList(string reason, IEnumerable<string> modules)
        => Term.Tuple(Term.A(reason), Cons.From(modules.Select(Term.A)));

    internal Term StageLoaderOnLoad(PreparedBeamImage image)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            ThrowIfSourceClosing();
            if (!ReferenceEquals(image.Owner, this) || image.Consumed)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (loadedModules.ContainsKey(image.Name) && oldCode.ContainsKey(image.Name))
                return Term.Tuple(Term.A(BeamLoaderNames.NotPurged), Cons.From([Term.A(image.Name)]));
            if (loaderPendingImages.ContainsKey(image.Name))
                throw new NotSupportedException(BeamLoaderDiagnostics.ConcurrentPending);
            BeamOnLoadTicket ticket = StagePreparedOnLoad(image);
            loaderPendingImages[image.Name] = ticket;

            return Term.Tuple(Term.A(BeamLoaderNames.OnLoad), Cons.From([Term.A(image.Name)]));
        }
    }

    public async ValueTask<Term> InvokeLoaderOnLoad(ProcessContext context, Term module)
    {
        if (module is not Atom name)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BeamOnLoadTicket ticket;
        IDisposable code;
        lock (gate)
        {
            if (!loaderPendingImages.TryGetValue(name.Name, out ticket!))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            code = context.EnterSourceCode(ticket.CodeImage, ticket);
        }
        try
        {
            BeamLinkedCode linked = ticket.Prepared.Code;
            int label = linked.OnLoadEntryLabel!.Value;
            BeamLinkedFunction function = linked.Functions.Single(entry => entry.EntryLabel == label);

            return await BeamRegisterInterpreter.ExecuteLocal(
                context,
                linked,
                function,
                Array.Empty<Term>(),
                Array.Empty<Term>(),
                ticket.Prepared.ExecutionIdentity
            );
        }
        finally
        {
            code.Dispose();
            context.RestoreSourceCodeFlow(code);
        }
    }

    internal Term FinishLoaderOnLoad(string module, bool keep)
    {
        using var source = keep ? EnterSourceMutation() : null;
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            if (keep)
                ThrowIfSourceClosing();
            if (!loaderPendingImages.TryGetValue(module, out BeamOnLoadTicket? ticket))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (!keep)
            {
                ticket.HasDeferredExports = false;

                return Term.A(ErlangBooleanAtoms.True);
            }
            if (loadedModules.ContainsKey(module) && oldCode.ContainsKey(module))
                throw new NotSupportedException(BeamOnLoadDiagnostics.OldGeneration);
            Apply(registry =>
            {
                if (loadedModules.ContainsKey(module))
                    registry.DeleteModule(module);
                if (ticket.HasDeferredExports)
                    for (int index = 0; index < ticket.Prepared.Code.Exports.Count; index++)
                    {
                        BeamLinkedFunction function = ticket.Prepared.Code.Exports[index];
                        registry.RegisterSource(
                            module,
                            function.Name,
                            function.Arity,
                            ticket.Prepared.Bodies[index],
                            ticket.Prepared.ExecutionIdentity.Owner
                        );
                    }
                registry.PublishModuleCode(module, ticket.Prepared.Descriptor);
                codeImages[module] = ticket.CodeImage;
            });
            ticket.CurrentPhase = BeamOnLoadPhase.Published;
            loaderPendingImages.Remove(module);
            pendingBeamImages.Remove(module);

            return Term.A(ErlangBooleanAtoms.True);
        }
    }
}
