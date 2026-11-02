namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RequireSourceLiteralAwait819()
    {
        if (Monitor.IsEntered(gate) || CodeModification.IsOwned)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        owningRuntime?.RequireModuleLiteralPreparation800();
    }

    internal ModuleCodeImage CreateSourceLiteralImage819(string module)
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        if (functions.Keys.Any(key => key.Module == module))
            throw new NotSupportedException(SourceLiteral819Names.CompleteModule);
        // A native source binding never mutates a pre-existing placeholder's
        // literal fields. Apply rollback restores its exact original image.
        var image = new ModuleCodeImage(module);
        codeImages[module] = image;

        return image;
    }

    internal void TrackSourceLiteralPublication819(PreparedSourceLiteralView819 view)
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        (ModuleRegistryApplyFrame.Find(this) ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid)).TrackSourceLiteral819(view);
    }

    internal void SettleSourceLiteralPublications819(IReadOnlyList<PreparedSourceLiteralView819> views)
    {
        var failures = new List<Exception>(views.Count);
        foreach (PreparedSourceLiteralView819 view in views)
            try
            {
                bool retained;
                lock (gate)
                {
                    ModuleCodeImage? image = view.Image819;
                    retained = image is not null && (codeImages.Values.Any(value => ReferenceEquals(value, image))
                        || oldCode.Values.Any(value => ReferenceEquals(value.Image, image))
                        || SavedOldImages432().Any(value => ReferenceEquals(value, image)));
                }
                if (!retained) view.RollbackPublication819();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    internal ProcessRuntime RequireSourceLiteralPreparation819()
    {
        if (Monitor.IsEntered(gate) || CodeModification.IsOwned)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        ProcessRuntime runtime = owningRuntime ?? throw new NotSupportedException(SourceLiteral819Names.RuntimeRequired);
        runtime.RequireModuleLiteralPreparation800();

        return runtime;
    }

    internal PreparedSourceLiteralView819 PrepareSourceLiteralView819(object templateOwner, IReadOnlyList<Term> templates, IReadOnlyList<Pattern.Literal> patterns)
    {
        ArgumentNullException.ThrowIfNull(templateOwner);
        ArgumentNullException.ThrowIfNull(templates);
        ProcessRuntime runtime = RequireSourceLiteralPreparation819();
        using var atoms = new AtomNameScope(runtime.AtomNames);
        using var source = EnterSourceMutation();
        ModuleLiteralDomain800 domain = ModuleLiteralDomain800.Prepare800(this, runtime, templates);
        try
        {
            return new(
                this,
                templateOwner,
                domain,
                templates,
                patterns
            );
        }
        catch
        {
            _ = domain.CloseCodeOwner800();
            throw;
        }
    }
}
