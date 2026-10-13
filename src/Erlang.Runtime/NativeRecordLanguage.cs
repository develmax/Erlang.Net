namespace Erlang;

public static class NativeRecordLanguage
{
    public static Term Definition(NativeRecordDefinition definition) => Term.Tuple(
        new MapTerm([new KeyValuePair<Term, Term>(Term.A(NativeRecordLanguageNames.ExportedOption), CoreModules.Boolean(definition.IsExported))]),
        Cons.From(definition.Fields.Select(field => field.Value is null ? (Term)field.Key : Term.Tuple(field.Key, field.Value)))
    );

    public static NativeRecordTerm Construct(
        string module,
        NativeRecordDefinition? definition,
        string name,
        IReadOnlyList<KeyValuePair<Atom, Term>> fields,
        bool external
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
            Term? value = supplied.TryGetValue(field.Key, out var explicitValue) ? explicitValue : field.Value;
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
        var record = Require(
            value,
            module,
            name,
            caller,
            external,
            true
        );
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
