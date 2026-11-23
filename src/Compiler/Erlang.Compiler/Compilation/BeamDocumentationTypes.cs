namespace Erlang.Compiler;

internal static class BeamDocumentationTypes
{
    internal static HashSet<(string Name, int Arity)> Exports(ModuleDefinition module) =>
        module.TypeForms.Where(form => form.Name == ModuleAttributeNames.ExportType)
            .SelectMany(form => SourceCallBindingBuilder.ReadSignatures(form.Value, 0))
            .ToHashSet();

    internal static (string Name, int Arity) Key(TupleTerm definition) =>
        (((Atom)definition.Items[0]).Name, Cons.Items(definition.Items[2]).Count());

    internal static string Signature(TupleTerm definition, (string Name, int Arity) key)
    {
        string name = BeamDocumentationWriter.FormatFunctionName(key.Name);
        var parameters = Cons.Items(definition.Items[2])
            .Select(item => ((Atom)((TupleTerm)item).Items[2]).Name).ToArray();

        return parameters.Contains("_", StringComparer.Ordinal)
            ? FormattableString.Invariant($"{name}/{key.Arity}")
            : name + "(" + string.Join(", ", parameters.Select(parameter => parameter.TrimStart('_'))) + ")";
    }

    internal static ILookup<string, (string Name, Term Type)> RecordFields(ModuleDefinition module) =>
        module.TypeForms.Where(form => form.Name == ModuleAttributeNames.Record)
            .Select(form => ((TupleTerm)form.Value).Items)
            .ToLookup(
                fields => ((Atom)fields[0]).Name,
                fields => (((Atom)fields[1]).Name, fields[2]),
                StringComparer.Ordinal
            );

    internal static HashSet<(string Name, int Arity)> Reachable(
        IReadOnlyList<ModuleAttribute> definitions,
        IReadOnlySet<(string Name, int Arity)> typeExports,
        IEnumerable<Parser.ReleaseSpec> publicSpecs,
        ILookup<string, (string Name, Term Type)> recordFields
    )
    {
        var dependencies = definitions.Where(form => form.Name == "type")
            .ToDictionary(form => Key((TupleTerm)form.Value), form => References(((TupleTerm)form.Value).Items[1], recordFields));
        var pending = new Stack<(string Name, int Arity)>(typeExports);
        foreach (var spec in publicSpecs)
        {
            foreach (var reference in References(spec.Form, recordFields))
            {
                pending.Push(reference);
            }
        }

        var reachable = new HashSet<(string Name, int Arity)>();
        while (pending.TryPop(out var key))
        {
            if (!reachable.Add(key) || !dependencies.TryGetValue(key, out var references))
            {
                continue;
            }
            foreach (var reference in references)
            {
                pending.Push(reference);
            }
        }

        return reachable;
    }

    internal static IEnumerable<(string Name, int Arity)> References(
        Term term,
        ILookup<string, (string Name, Term Type)> recordFields
    )
    {
        var expandedRecords = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reference in Read(term))
        {
            yield return reference;
        }

        IEnumerable<(string Name, int Arity)> Read(Term value)
        {
            if (value is TupleTerm tuple)
            {
                if (tuple.Items is
                    [
                        Atom { Name: NativeRecordTypeNames.Type }, _, Atom { Name: NativeRecordTypeNames.Record },
                        Cons { Head: TupleTerm { Items: [Atom { Name: NativeRecordTypeNames.Atom }, _, Atom record] }, Tail: var overrides }
                    ])
                {
                    // OTP removes the record's defaults before traversing its fields.
                    // Explicit field types win and are traversed on every occurrence.
                    var declared = expandedRecords.Add(record.Name)
                        ? recordFields[record.Name]
                        : Enumerable.Empty<(string Name, Term Type)>();
                    var explicitFields = new HashSet<string>(StringComparer.Ordinal);
                    foreach (Term item in Cons.Items(overrides))
                    {
                        if (item is TupleTerm
                            {
                                Items:
                                [
                                    Atom { Name: NativeRecordTypeNames.Type }, _, Atom { Name: NativeRecordTypeNames.Field },
                                    Cons { Head: TupleTerm { Items: [Atom { Name: NativeRecordTypeNames.Atom }, _, Atom field] }, Tail: var fieldType }
                                ]
                            } && explicitFields.Add(field.Name))
                        {
                            foreach (var reference in Read(fieldType))
                            {
                                yield return reference;
                            }
                        }
                    }

                    foreach (var field in declared)
                    {
                        if (explicitFields.Contains(field.Name))
                        {
                            continue;
                        }

                        foreach (var reference in Read(field.Type))
                        {
                            yield return reference;
                        }
                    }

                    yield break;
                }

                if (tuple.Items is [Atom { Name: NativeRecordTypeNames.Local }, _, Atom name, var arguments])
                {
                    yield return (name.Name, Cons.Items(arguments).Count());
                }

                foreach (Term item in tuple.Items)
                {
                    foreach (var reference in Read(item))
                    {
                        yield return reference;
                    }
                }
            }
            else if (value is Cons)
            {
                foreach (Term item in Cons.Items(value))
                {
                    foreach (var reference in Read(item))
                    {
                        yield return reference;
                    }
                }
            }
        }
    }
}
