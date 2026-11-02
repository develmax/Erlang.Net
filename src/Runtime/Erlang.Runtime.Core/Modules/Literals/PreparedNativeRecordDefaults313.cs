namespace Erlang;

internal sealed class PreparedNativeRecordDefaults313
{
    private readonly ModuleCodeImage image;
    private readonly ModuleLiteralDomain800 domain;
    private readonly IReadOnlyDictionary<Term, Term> values;
    private readonly HashSet<NativeRecordDefinition> definitions;

    internal PreparedNativeRecordDefaults313(
        ModuleCodeImage image,
        ModuleLiteralDomain800 domain,
        IReadOnlyList<Term> templates,
        IReadOnlyList<NativeRecordDefinition> definitions
    )
    {
        this.image = image;
        this.domain = domain;
        int first = domain.Literals800.Count - templates.Count;
        var entries = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < templates.Count; index++)
            entries.Add(templates[index], domain.Literals800[first + index]);
        NativeRecordDefinition[] snapshot = definitions.ToArray();
        foreach (NativeRecordDefinition definition in snapshot)
            foreach (var field in definition.Fields)
                if (field.Value is { } value && !entries.ContainsKey(value))
                    throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        values = entries;
        this.definitions = new(snapshot, ReferenceEqualityComparer.Instance);
    }

    internal bool OwnsDefinition323(NativeRecordDefinition definition, ModuleCodeImage actualImage, ModuleLiteralLease800 lease)
        => ReferenceEquals(image, actualImage) && domain.HasSourceLease819(lease) && definitions.Contains(definition);

    internal Term Read(Term template, ModuleCodeImage actualImage, ModuleLiteralLease800 lease)
    {
        if (!ReferenceEquals(image, actualImage) || !domain.HasSourceLease819(lease))
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        if (values.TryGetValue(template, out Term? value)) return value;
        throw new NotSupportedException(SourceLiteral819Names.Unprepared);
    }
}
