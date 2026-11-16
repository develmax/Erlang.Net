using System.Text;

namespace Erlang.Compiler;

internal static class BeamDocumentationWriter
{
    internal static byte[]? Build(
        ModuleDefinition module,
        IReadOnlyList<Token> tokens,
        string source,
        IReadOnlySet<(string Name, int Arity)> exports,
        IEnumerable<Parser.ReleaseSpec> specs,
        string? releaseSourcePath = null,
        IReadOnlyDictionary<string, string>? releaseIncludeFiles = null
    )
    {
        if (module.Documentation.Count == 0 && !module.HasGeneratedEunitTest)
        {
            return null;
        }

        var specForms = specs.ToArray();
        var definitions = module.TypeForms.Where(form => Parser.IsTypeDefinition(form.Name)).ToArray();
        var typeExports = BeamDocumentationTypes.Exports(module);
        var callbacks = BeamSourceForms.Starts(tokens)
            .Where(index => tokens[index].Text == ErlangSyntaxTokens.AttributeIntroducer && tokens[index + 1].Text == "callback")
            .ToDictionary(index => index, index => Parser.ReadReleaseSpec(source, tokens, index + 2));
        var signatures = specForms.Concat(callbacks.Values).Where(spec => spec.Signature is not null)
            .OrderBy(spec => spec.SourceIndex)
            .GroupBy(spec => spec.Function)
            .ToDictionary(group => group.Key, group => group.Last().Signature);
        var exportedSpecs = specForms.Where(spec => exports.Contains(spec.Function)).ToLookup(spec => spec.Function);
        var publicSpecs = exportedSpecs.SelectMany(group => group).Concat(callbacks.Values);
        var reachableTypes = BeamDocumentationTypes.Reachable(definitions, typeExports, publicSpecs);
        Term? moduleAnnotation = null;
        Term docAnnotation = Term.I(1);
        Term moduleDoc = Term.A("none");
        (Term Annotation, Term Value)? pending = null;
        var moduleMetadata = new Dictionary<Term, Term>();
        foreach (var form in module.Documentation.Where(form => form.Name == ModuleAttributeNames.ModuleDoc))
        {
            BeamDocumentationMetadata.Merge(moduleMetadata, form);
        }
        var pendingMetadata = new Dictionary<Term, Term>();
        var deprecations = new BeamDocumentationDeprecations(module.Attributes);
        int docIndex = 0;
        int functionIndex = module.DiameterAccessorLines.Count;
        int typeIndex = 0;
        var typeEntries = new List<((string Name, int Arity) Key, Term Entry)>();
        var visibleReferences = new HashSet<(string Name, int Arity)>(typeExports);
        var callbackEntries = new List<Term>();
        var entries = new List<Term>();
        foreach (int index in BeamSourceForms.Starts(tokens))
        {
            Token token = tokens[index];
            if (token.Kind == LexerTokenKinds.Symbol && token.Text == ErlangSyntaxTokens.AttributeIntroducer)
            {
                Token attribute = tokens[index + 1];
                switch (attribute.Text)
                {
                    case ModuleAttributeNames.Module:
                        moduleAnnotation = Annotation(attribute, source);
                        break;
                    case ModuleAttributeNames.ModuleDoc:
                        var moduleForm = module.Documentation[docIndex++];
                        if (moduleForm.Value is not (MapTerm or Atom { Name: "true" }))
                        {
                            docAnnotation = EntryAnnotation(attribute, Annotation(attribute, source), releaseIncludeFiles);
                            moduleDoc = BeamDocumentationText.Read(moduleForm.Value).Value;
                        }
                        break;
                    case ModuleAttributeNames.Doc:
                        var form = module.Documentation[docIndex++];
                        if (form.Value is MapTerm)
                        {
                            BeamDocumentationMetadata.Merge(pendingMetadata, form);
                        }
                        else if (form.Value is Atom { Name: "false" or "hidden" }
                            || form.Value is not Atom { Name: "true" }
                                && pending?.Value is not Atom { Name: "false" or "hidden" })
                        {
                            pending = (EntryAnnotation(attribute, Annotation(attribute, source), releaseIncludeFiles), form.Value);
                        }
                        break;
                    case "callback":
                        var callback = callbacks[index];
                        Term callbackLocation = Annotation(attribute, source);
                        var (callbackSignature, callbackDoc) = ReadEntry(pending?.Value, callback.Function);
                        string callbackName = FormatFunctionName(callback.Function.Name);
                        string signatureText = callbackSignature ?? (signatures.GetValueOrDefault(callback.Function) is { } parsedSignature
                            ? callbackName + parsedSignature[callback.Function.Name.Length..]
                            : $"{callbackName}/{callback.Function.Arity}");
                        if (callbackDoc is not Atom { Name: "hidden" })
                        {
                            visibleReferences.UnionWith(BeamDocumentationTypes.References(callback.Form));
                        }
                        callbackEntries.Add(Term.Tuple(
                            Term.Tuple(Term.A("callback"), Term.A(callback.Function.Name), Term.I(callback.Function.Arity)),
                            pending?.Annotation ?? EntryAnnotation(attribute, callbackLocation, releaseIncludeFiles),
                            Cons.From([Binary(signatureText)]),
                            callbackDoc,
                            EntryMetadata("callback", callback.Function, callbackLocation)
                        ));
                        pending = null;
                        pendingMetadata.Clear();
                        break;
                    case string name when Parser.IsTypeDefinition(name):
                        var definition = (TupleTerm)definitions[typeIndex++].Value;
                        var key = BeamDocumentationTypes.Key(definition);
                        var (signature, doc) = ReadEntry(pending?.Value, key);
                        if (doc is not Atom { Name: "hidden" })
                        {
                            visibleReferences.UnionWith(BeamDocumentationTypes.References(definition.Items[1]));
                        }
                        if (reachableTypes.Contains(key))
                        {
                            Term location = Annotation(attribute, source);
                            Term entryLocation = EntryAnnotation(attribute, location, releaseIncludeFiles);
                            typeEntries.Add((key, Term.Tuple(
                                Term.Tuple(Term.A("type"), Term.A(key.Name), Term.I(key.Arity)),
                                pending?.Annotation ?? entryLocation,
                                Cons.From([Binary(signature ?? BeamDocumentationTypes.Signature(definition, key))]),
                                doc,
                                EntryMetadata("type", key, location, typeExports.Contains(key))
                            )));
                        }
                        pending = null;
                        pendingMetadata.Clear();
                        break;
                }
                continue;
            }

            var function = module.DeclaredFunctions[functionIndex++];
            if (!exports.Contains((function.Name, function.Arity)))
            {
                pending = null;
                pendingMetadata.Clear();
                continue;
            }
            Term functionLocation = Annotation(token, source);
            var functionKey = (function.Name, function.Arity);
            var (functionSignature, functionDoc) = ReadEntry(pending?.Value, functionKey);
            if (functionDoc is not Atom { Name: "hidden" })
            {
                foreach (Parser.ReleaseSpec spec in exportedSpecs[functionKey])
                {
                    visibleReferences.UnionWith(BeamDocumentationTypes.References(spec.Form));
                }
            }
            string functionSignatureText = functionSignature ?? (signatures.GetValueOrDefault(functionKey) is { } parsedSpec
                ? FormatFunctionName(function.Name) + parsedSpec[function.Name.Length..]
                : Signature(function));
            entries.Add(Term.Tuple(
                Term.Tuple(Term.A("function"), Term.A(function.Name), Term.I(function.Arity)),
                pending?.Annotation ?? EntryAnnotation(token, functionLocation, releaseIncludeFiles),
                Cons.From([Binary(functionSignatureText)]),
                functionDoc,
                EntryMetadata("function", functionKey, functionLocation)
            ));
            pending = null;
            pendingMetadata.Clear();
        }
        if (moduleAnnotation is null || pending is not null || pendingMetadata.Count != 0)
        {
            throw Unsupported();
        }
        if (module.HasGeneratedEunitTest)
        {
            Term zero = Term.I(0);
            entries.Add(Term.Tuple(
                Term.Tuple(Term.A("function"), Term.A(EunitAutoExport.Test), zero),
                zero, Term.List(Binary("test()")), Term.A("hidden"),
                EntryMetadata("function", (EunitAutoExport.Test, 0), zero)
            ));
        }
        moduleMetadata.TryAdd(Term.A("otp_doc_vsn"), Term.Tuple(Term.I(1), Term.I(0), Term.I(0)));
        moduleMetadata[Term.A("behaviours")] = Cons.From(module.Attributes.Where(IsBehaviour)
            .Select(attribute => attribute.Value).Order());
        moduleMetadata[Term.A("source_anno")] = moduleAnnotation;
        if (releaseSourcePath is not null)
        {
            moduleMetadata[Term.A("source_path")] = Term.String(releaseSourcePath);
        }
        var document = Term.Tuple(
            Term.A("docs_v1"),
            docAnnotation,
            Term.A("erlang"),
            moduleMetadata.GetValueOrDefault(Term.A("format")) ?? Binary("text/markdown"),
            moduleDoc,
            new MapTerm(moduleMetadata),
            Cons.From(typeEntries.AsEnumerable().Reverse()
                .Where(type => visibleReferences.Contains(type.Key)).Select(type => type.Entry)
                .Concat(callbackEntries.AsEnumerable().Reverse()).Concat(entries.AsEnumerable().Reverse()))
        );

        return BeamOtpEtfCompression.Encode(document);

        Term EntryMetadata(string kind, (string Name, int Arity) key, Term location, bool? exported = null)
        {
            deprecations.Apply(pendingMetadata, module.Name, kind, key);
            return BeamDocumentationMetadata.Entry(pendingMetadata, moduleMetadata, location, exported);
        }
    }

    private static (string? Signature, Term Value) ReadEntry(Term? value, (string Name, int Arity) key) =>
        value is null ? (null, Term.A("none")) : BeamDocumentationText.Read(value, key);

    internal static Term Annotation(Token token, string source)
    {
        int beginning = token.PreprocessorColumnStart
            ?? (token.Start == 0 ? 0 : source.LastIndexOf('\n', token.Start - 1) + 1);
        int column = 1;
        foreach (var _ in source.AsSpan(beginning, token.Start - beginning).EnumerateRunes())
        {
            column++;
        }

        return Term.Tuple(Term.I(token.PreprocessorLine), Term.I(column));
    }

    private static Term EntryAnnotation(
        Token attribute, Term location, IReadOnlyDictionary<string, string>? releaseIncludeFiles
    )
    {
        if (attribute.PreprocessorFile is not { } file)
        {
            return location;
        }

        int separator = Math.Max(file.LastIndexOf('/'), file.LastIndexOf('\\'));

        string recordedFile = releaseIncludeFiles?.GetValueOrDefault(file)
            ?? (attribute.HasExplicitFileLocation ? file : file[(separator + 1)..]);
        return Term.List(
            Term.Tuple(Term.A("file"), Term.String(recordedFile)),
            Term.Tuple(Term.A("location"), location)
        );
    }

    private static BitString Binary(string value) => new(Encoding.UTF8.GetBytes(value));

    private static bool IsBehaviour(ModuleAttribute attribute) =>
        attribute.Name is "behaviour" or "behavior" && attribute.Value is Atom;

    private static string Signature(FunctionDefinition function)
    {
        if (function.Clauses is not [var clause])
        {
            return AritySignature(function);
        }
        var names = new List<string>();
        foreach (var pattern in clause.Patterns)
        {
            if (pattern is not Pattern.Variable variable || variable.Name.TrimStart('_').Length == 0)
            {
                return AritySignature(function);
            }
            names.Add(variable.Name.TrimStart('_'));
        }

        return FormatFunctionName(function.Name) + "(" + string.Join(", ", names) + ")";
    }

    private static string AritySignature(FunctionDefinition function) =>
        FormatFunctionName(function.Name) + "/" + function.Arity.ToString(System.Globalization.CultureInfo.InvariantCulture);

    internal static string FormatFunctionName(string name) =>
        Lexer.IsKeyword(name) ? "'" + name + "'" : Term.A(name).ToString()!;

    private static NotSupportedException Unsupported() => new(BeamEmissionFormat.Unsupported);
}
