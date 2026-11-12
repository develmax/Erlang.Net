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

    internal static HashSet<(string Name, int Arity)> Reachable(
        IReadOnlyList<ModuleAttribute> definitions,
        IReadOnlySet<(string Name, int Arity)> typeExports,
        IEnumerable<Parser.ReleaseSpec> publicSpecs
    )
    {
        var dependencies = definitions.Where(form => form.Name == "type")
            .ToDictionary(form => Key((TupleTerm)form.Value), form => References(((TupleTerm)form.Value).Items[1]));
        var pending = new Stack<(string Name, int Arity)>(typeExports);
        foreach (var spec in publicSpecs)
        {
            foreach (var reference in References(spec.Form))
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

    internal static IEnumerable<(string Name, int Arity)> References(Term term)
    {
        if (term is TupleTerm tuple)
        {
            if (tuple.Items is [Atom { Name: NativeRecordTypeNames.Local }, _, Atom name, var arguments])
            {
                yield return (name.Name, Cons.Items(arguments).Count());
            }
            foreach (Term item in tuple.Items)
            {
                foreach (var reference in References(item))
                {
                    yield return reference;
                }
            }
        }
        else if (term is Cons)
        {
            foreach (Term item in Cons.Items(term))
            {
                foreach (var reference in References(item))
                {
                    yield return reference;
                }
            }
        }
    }
}
