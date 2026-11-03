namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal async ValueTask<Term> ConstructNativeRecord313(
        ProcessContext context,
        string module,
        string name,
        IReadOnlyList<KeyValuePair<Atom, Term>> fields
    )
    {
        var selected = await AcquireNativeRecord313(context, module, name);
        using SourceCodeLease? lease = selected.Lease;
        Term result = NativeRecordLanguage.Construct313(
            module,
            selected.Definition,
            name,
            fields,
            true,
            template => ReadNativeRecordDefault313(
                context,
                template,
                lease,
                selected.Definition
            )
        );

        // Publish actual process/literal custody before the temporary current
        // image lease ends. Direct standalone host calls keep portable metadata.
        return context.OwnEvaluationResult507(result);
    }

    internal async ValueTask<Term> NativeRecordDefinition313(ProcessContext context, string module, string name)
    {
        var selected = await AcquireNativeRecord313(context, module, name);
        using SourceCodeLease? lease = selected.Lease;
        if (selected.Definition is not { } definition)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term result = NativeRecordLanguage.Definition313(definition, template => ReadNativeRecordDefault313(
            context,
            template,
            lease,
            definition
        ));
        if (lease is not null && lease.Image.HasPreparedNativeRecordDefinition323(definition, lease)
            && context.CurrentOwned506 is { } invocation && invocation.CanAdoptNativeRecordDefinition323(context))
        {
            // The actual selected image lease is still held here. Adopt only
            // this canonical body's fresh scaffold, keeping real literal cells
            // shared; generic opaque imports intentionally detach all children.
            return invocation.Frame.AdoptExpression511(result);
        }

        return context.OwnEvaluationResult507(result);
    }

    private static Term ReadNativeRecordDefault313(
        ProcessContext context,
        Term template,
        SourceCodeLease? lease,
        NativeRecordDefinition? definition
    )
        => context.CurrentOwned506 is null || lease is null || definition is null || !lease.Image.HasPreparedNativeRecordDefinition323(definition, lease)
            ? template : lease.Image.ReadNativeRecordDefault313(template, lease.LiteralLease800);

    private async ValueTask<(NativeRecordDefinition? Definition, SourceCodeLease? Lease)> AcquireNativeRecord313(
        ProcessContext context,
        string module,
        string name
    )
    {
        while (true)
        {
            Task? pending;
            lock (gate)
            {
                NativeRecordDefinition? definition = codeDescriptors.GetValueOrDefault(module)?.NativeRecords.FirstOrDefault(value => value.Name == name);
                ModuleCodeImage? image = codeImages.GetValueOrDefault(module);
                if (definition is null || image is null) return (definition, null);
                var state = purgeStates.GetOrCreateValue(image);
                if (state.Retired) return (null, null);
                pending = state.Pending?.Task;
                if (pending is null || context.IsExecutingCode(image))
                    return (definition, context.ReserveSourceCode(image));
            }
            // Reselect both current definition AND image after pending purge.
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);
        }
    }
}
