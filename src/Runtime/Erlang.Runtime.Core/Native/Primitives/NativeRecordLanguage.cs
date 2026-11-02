namespace Erlang;

public static class NativeRecordLanguage
{
    public static Term Definition(NativeRecordDefinition definition) => Definition313(definition, null);

    internal static Term Definition313(NativeRecordDefinition definition, Func<Term, Term>? readDefault) => Term.Tuple(
        new MapTerm([new KeyValuePair<Term, Term>(Term.A(NativeRecordLanguageNames.ExportedOption), CoreModules.Boolean(definition.IsExported))]),
        Cons.From(definition.Fields.Select(field => field.Value is null ? (Term)field.Key : Term.Tuple(field.Key, readDefault is null ? field.Value : readDefault(field.Value))))
    );

    public static NativeRecordTerm Construct(
        string module,
        NativeRecordDefinition? definition,
        string name,
        IReadOnlyList<KeyValuePair<Atom, Term>> fields,
        bool external
    ) => Construct313(
        module,
        definition,
        name,
        fields,
        external,
        null
    );

    internal static NativeRecordTerm Construct313(
        string module,
        NativeRecordDefinition? definition,
        string name,
        IReadOnlyList<KeyValuePair<Atom, Term>> fields,
        bool external,
        Func<Term, Term>? readDefault
    )
    {
        var identity = Term.Tuple(Term.A(module), Term.A(name));
        if (definition is null || external && !definition.IsExported)
            throw Failure(NativeRecordLanguageNames.BadRecord, identity);
        var supplied = fields.ToDictionary(field => field.Key, field => field.Value);
        var declared = definition.Fields.Select(field => field.Key).ToHashSet();
        var unknown = fields.Where(field => !declared.Contains(field.Key)).OrderBy(field => AtomNameRegistry.IndexOf(field.Key.Name)).FirstOrDefault();
        if (unknown.Key is not null)
            throw Failure(NativeRecordLanguageNames.BadField, Term.Tuple(identity, unknown.Key));
        var values = new List<KeyValuePair<Atom, Term>>();
        foreach (var field in definition.Fields)
        {
            Term? value;
            if (supplied.TryGetValue(field.Key, out var explicitValue)) value = explicitValue;
            else value = field.Value is { } template && readDefault is not null ? readDefault(template) : field.Value;
            if (value is null)
                throw Failure(NativeRecordLanguageNames.NoValue, Term.Tuple(identity, field.Key));
            values.Add(new(field.Key, value));
        }

        return new NativeRecordTerm(
            Term.A(module),
            Term.A(name),
            definition.IsExported,
            values
        );
    }

    public static NativeRecordTerm Require(
        Term value,
        string module,
        string? name,
        string caller,
        bool external,
        bool fields
    )
    {
        if (value is not NativeRecordTerm record || name is not null && (record.Module.Name != module || record.Name.Name != name) ||
            fields && (external || name is null && record.Module.Name != caller) && !record.IsExported)
            throw Failure(NativeRecordLanguageNames.BadRecord, value);

        return record;
    }

    public static Term Access(
        Term value,
        string module,
        string? name,
        string caller,
        bool external,
        Atom field
    )
    {
        NativeRecordTerm record;
        if (external)
            record = Require(
                value,
                module,
                name,
                caller,
                external,
                true
            );
        else
        {
            if (value is not NativeRecordTerm local ||
                name is not null && name != NativeRecordLanguageNames.LocalAccessWildcard && local.Name.Name != name)
                throw Failure(NativeRecordLanguageNames.BadRecord, value);
            record = local;
        }
        for (int index = 0; index < record.FieldNames.Count; index++)
        {
            if (record.FieldNames[index].Equals(field))
                return record.Values[index];
        }

        throw Failure(NativeRecordLanguageNames.BadField, Term.Tuple(Term.Tuple(record.Module, record.Name), field));
    }

    public static Term Update(
        Term value,
        string module,
        string? name,
        string caller,
        bool external,
        IReadOnlyList<KeyValuePair<Atom, Term>> fields
    )
    {
        var record = Require(
            value,
            module,
            name,
            caller,
            external,
            true
        );
        var updates = fields.ToDictionary(field => field.Key, field => field.Value);
        foreach (var field in fields.OrderBy(field => AtomNameRegistry.IndexOf(field.Key.Name)))
        {
            if (!record.FieldNames.Contains(field.Key))
                throw Failure(NativeRecordLanguageNames.BadField, Term.Tuple(Term.Tuple(record.Module, record.Name), field.Key));
        }

        return fields.Count == 0 ? record : new NativeRecordTerm(
            record.Module,
            record.Name,
            record.IsExported,
            record.FieldNames.Select((field, index) => new KeyValuePair<Atom, Term>(field, updates.TryGetValue(field, out var changed) ? changed : record.Values[index]))
        );
    }

    private static ErlangException Failure(string reason, Term value) => new(Term.Tuple(Term.A(reason), value));
}
