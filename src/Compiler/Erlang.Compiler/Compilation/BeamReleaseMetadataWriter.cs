namespace Erlang.Compiler;

internal static class BeamReleaseMetadataWriter
{
    internal static BeamModuleMetadata Build(
        ModuleDefinition module,
        IReadOnlyList<Token> tokens,
        string source,
        BeamReleaseBuildProfile profile,
        byte[]? documentation,
        IReadOnlyDictionary<int, Parser.ReleaseSpec> specs,
        IReadOnlyList<BeamFileTransition> fileTransitions,
        Parser parser
    )
    {
        try
        {
            return BuildMetadata(module, tokens, source, profile, documentation, specs, fileTransitions, parser);
        }
        catch (NotSupportedException) when (profile.AllowIncompleteAbstractCode)
        {
            IReadOnlyList<ModuleAttribute> attributes = profile.InsertedVersion is { } version
                ? [new(profile.InsertedAttributeName, Term.String(version)), .. module.Attributes]
                : module.Attributes;
            return new(attributes, documentation, WriteCompileInfo(profile));
        }
    }

    private static BeamModuleMetadata BuildMetadata(
        ModuleDefinition module,
        IReadOnlyList<Token> tokens,
        string source,
        BeamReleaseBuildProfile profile,
        byte[]? documentation,
        IReadOnlyDictionary<int, Parser.ReleaseSpec> specs,
        IReadOnlyList<BeamFileTransition> fileTransitions,
        Parser parser
    )
    {
        if (!profile.EmitAbstractCode)
        {
            if (profile.RecordedCompileOptions is null || profile.InsertedVersion is not null)
            {
                throw new InvalidOperationException("A compile-info-only profile requires explicit options and no attribute insertion.");
            }

            return new(module.Attributes, documentation, CompileInfo(profile, profile.RecordedCompileOptions));
        }

        string mainFile = profile.RecordedSourceFile
            ?? (profile.RecordSourceFileInLineTable ? profile.SourcePath : Path.GetFileName(profile.SourcePath));
        int directoryEnd = Math.Max(mainFile.LastIndexOf('/'), mainFile.LastIndexOf('\\')) + 1;
        var annotations = new List<Term> { FileForm(mainFile, 1) };
        int transitionIndex = 0;
        void WriteFileTransitions(int index)
        {
            while (transitionIndex < fileTransitions.Count && fileTransitions[transitionIndex].TokenIndex == index)
            {
                var transition = fileTransitions[transitionIndex++];
                if (transition.Directive is { } directive)
                {
                    Term generated = Term.List(
                        Term.Tuple(Term.A("generated"), Term.A("true")),
                        Term.Tuple(Term.A("location"), BeamDocumentationWriter.Annotation(directive, source))
                    );
                    annotations.Add(Attribute(generated, "file", Term.Tuple(Term.String(transition.File!), Term.I(transition.Line))));
                    continue;
                }
                string file = transition.File is null
                    ? mainFile
                    : (transition.IncludeRequest is { } request ? profile.RecordedIncludeFiles?.GetValueOrDefault(request) : null)
                        ?? profile.RecordedIncludeFiles?.GetValueOrDefault(transition.File)
                        ?? (mainFile[..directoryEnd] + transition.File);
                annotations.Add(FileForm(file, transition.Line, transition.Column));
            }
        }
        var attributes = new List<ModuleAttribute>();
        var typeDefinitions = new Queue<ModuleAttribute>(module.TypeForms.Where(form => Parser.IsTypeDefinition(form.Name)));
        var optionalCallbacks = new Queue<ModuleAttribute>(module.TypeForms.Where(form => form.Name == "optional_callbacks"));
        int attributeIndex = 0;
        int functionIndex = module.DiameterAccessorLines.Count;
        bool writeDiameterAccessors = functionIndex != 0;
        foreach (int index in BeamSourceForms.Starts(tokens))
        {
            WriteFileTransitions(index);
            Token start = tokens[index];
            if (start.Kind != LexerTokenKinds.Symbol || start.Text != ErlangSyntaxTokens.AttributeIntroducer)
            {
                if (writeDiameterAccessors)
                {
                    annotations.AddRange(BeamDiameterAbstractForms.Write(module.DeclaredFunctions.Take(functionIndex).ToArray()));
                    writeDiameterAccessors = false;
                }

                var function = module.DeclaredFunctions[functionIndex++];
                try
                {
                    annotations.Add(BeamReleaseFunctionDebug.Write(function, tokens, index, source, parser));
                }
                catch (NotSupportedException error)
                {
                    throw new NotSupportedException($"Cannot write debug forms for {function.Name}/{function.Arity}. {error.Message}", error);
                }
                continue;
            }
            Token name = tokens[index + 1];
            Term location = BeamDocumentationWriter.Annotation(name, source);
            switch (name.Text)
            {
                case ModuleAttributeNames.Module:
                    annotations.Add(Attribute(location, name.Text, Term.A(module.Name)));
                    if (profile.InsertedVersion is not null)
                    {
                        Term version = Term.String(profile.InsertedVersion);
                        annotations.Add(Attribute(location, profile.InsertedAttributeName, version));
                        attributes.Add(new(profile.InsertedAttributeName, version));
                    }
                    for (int transform = 1; transform <= module.EunitAutoExportCount; transform++)
                    {
                        IEnumerable<Term> exports = module.EunitExports.Select(signature =>
                            Term.Tuple(Term.A(signature.Name), Term.I(signature.Arity)));
                        if (module.HasGeneratedEunitTest && transform == module.EunitAutoExportCount)
                        {
                            exports = exports.Prepend(Term.Tuple(Term.A(EunitAutoExport.Test), Term.I(0)));
                        }
                        annotations.Add(Attribute(Term.I(0), ModuleAttributeNames.Export, Cons.From(exports)));
                    }
                    break;
                case ModuleAttributeNames.ModuleDoc:
                case ModuleAttributeNames.Doc:
                    break;
                case "spec":
                    annotations.Add(Attribute(location, name.Text, specs[index].Form));
                    break;
                case "callback":
                    annotations.Add(Attribute(location, name.Text, Parser.ReadReleaseSpec(source, tokens, index + 2).Form));
                    break;
                case "optional_callbacks":
                    annotations.Add(Attribute(location, name.Text, optionalCallbacks.Dequeue().Value));
                    break;
                case string type when Parser.IsTypeDefinition(type):
                    ModuleAttribute definition = typeDefinitions.Dequeue();
                    annotations.Add(Attribute(location, type, definition.Value));
                    if (type == "nominal")
                    {
                        attributes.Add(definition);
                        attributeIndex++;
                    }
                    break;
                case ModuleAttributeNames.Export:
                case ModuleAttributeNames.ExportType:
                case ModuleAttributeNames.Nifs:
                case ModuleAttributeNames.OnLoad:
                    annotations.Add(Attribute(location, name.Text, ReadExport(tokens, index + 2, source)));
                    break;
                case SourceCallBindingNames.Import:
                    annotations.Add(Attribute(location, name.Text, Parser.ReadReleaseImport(source, tokens, index + 2)));
                    break;
                case ModuleAttributeNames.Compile when !module.HasOtherCompileOptions:
                    Term options = ReadExport(tokens, index + 2, source);
                    if (!IsExpandedTransform(options))
                    {
                        if (options is Cons)
                        {
                            options = Cons.From(Cons.Items(options).Where(option => !IsExpandedTransform(option)));
                        }
                        annotations.Add(Attribute(location, name.Text, options));
                    }
                    break;
                case ModuleAttributeNames.Record:
                    annotations.Add(Attribute(location, name.Text, BeamReleaseFunctionDebug.WriteRecord(tokens, index + 2, source, parser)));
                    break;
                default:
                    if (attributeIndex == module.Attributes.Count || module.Attributes[attributeIndex].Name != name.Text)
                    {
                        throw new NotSupportedException(BeamEmissionFormat.Unsupported);
                    }
                    ModuleAttribute attribute = module.Attributes[attributeIndex++];
                    annotations.Add(Attribute(location, attribute.Name, attribute.Value));
                    attributes.Add(attribute);
                    break;
            }
        }
        WriteFileTransitions(tokens.Count - 1);
        if (writeDiameterAccessors)
        {
            annotations.AddRange(BeamDiameterAbstractForms.Write(module.DeclaredFunctions.Take(functionIndex).ToArray()));
        }

        annotations.Add(Term.Tuple(Term.A("eof"), BeamDocumentationWriter.Annotation(tokens[^1], source)));

        if (module.HasGeneratedEunitTest)
        {
            annotations.Add(EunitAutoExport.AbstractWrapper(module.Name));
        }

        Term debugOption = Term.A("debug_info");
        Term[] includes = profile.IncludeDirectories.Select(path => (Term)Term.Tuple(Term.A("i"), Term.String(path))).ToArray();
        Term[] warnings = profile.WarningOptions.Select(option => (Term)Term.A(option)).ToArray();
        var debugOptions = new List<Term> { Term.Tuple(Term.A("cwd"), Term.String(profile.WorkingDirectory)) };
        debugOptions.AddRange(includes);
        debugOptions.Add(debugOption);
        int beforeInsertion = profile.WarningsBeforeInsertion ?? warnings.Length;
        debugOptions.AddRange(warnings.Take(beforeInsertion));
        if (profile.InsertedVersion is not null)
        {
            debugOptions.Add(InsertedAttribute(profile));
        }
        debugOptions.AddRange(warnings.Skip(beforeInsertion));

        byte[] compileInfo = WriteCompileInfo(profile);
        byte[] debugInfo = BeamOtpEtfCompression.Encode(Term.Tuple(
            Term.A("debug_info_v1"),
            Term.A("erl_abstract_code"),
            Term.Tuple(Cons.From(annotations), Cons.From(profile.RecordedDebugOptions ?? debugOptions))
        ), debugInfo: true);

        return new(attributes, documentation, compileInfo, debugInfo);
    }

    private static bool IsExpandedTransform(Term option) =>
        EunitAutoExport.IsTransform(option) || DiameterRecordSourceTransform.IsOption(option) || MatchSpecSourceTransform.IsOption(option);

    private static Term InsertedAttribute(BeamReleaseBuildProfile profile) =>
        Term.Tuple(Term.A("attribute"), Term.A("insert"), Term.A(profile.InsertedAttributeName), Term.String(profile.InsertedVersion!));

    private static byte[] WriteCompileInfo(BeamReleaseBuildProfile profile)
    {
        if (profile.RecordedCompileOptions is { } recorded)
        {
            return CompileInfo(profile, recorded);
        }

        var options = new List<Term> { Term.A("debug_info") };
        if (profile.RecordedDefinitions is not null)
        {
            options.AddRange(profile.RecordedDefinitions.Select(entry => Term.Tuple(Term.A("d"), Term.A(entry.Key), entry.Value)));
        }

        options.AddRange(profile.IncludeDirectories.Select(path => Term.Tuple(Term.A("i"), Term.String(path))));
        int beforeInsertion = profile.WarningsBeforeInsertion ?? profile.WarningOptions.Count;
        options.AddRange(profile.WarningOptions.Take(beforeInsertion).Select(option => Term.A(option)));
        if (profile.InsertedVersion is not null)
        {
            options.Add(Term.Tuple(Term.A("parse_transform"), Term.A("sys_pre_attributes")));
            options.Add(InsertedAttribute(profile));
        }

        options.AddRange(profile.WarningOptions.Skip(beforeInsertion).Select(option => Term.A(option)));
        return CompileInfo(profile, options);
    }

    private static byte[] CompileInfo(BeamReleaseBuildProfile profile, IReadOnlyList<Term> options) =>
        ExternalTermFormat.Encode(Cons.From([
            Term.Tuple(Term.A("version"), Term.String(profile.CompilerVersion)),
            Term.Tuple(Term.A("options"), Cons.From(options)),
            Term.Tuple(Term.A("source"), Term.String(profile.SourcePath))
        ]));

    private static Term Attribute(Term annotation, string name, Term value) =>
        Term.Tuple(Term.A("attribute"), annotation, Term.A(name), value);

    private static Term FileForm(string file, int line, int column = 1) =>
        Attribute(Term.Tuple(Term.I(line), Term.I(column)), "file", Term.Tuple(Term.String(file), Term.I(line)));

    private static Term ReadExport(IReadOnlyList<Token> tokens, int start, string source)
    {
        var expression = new List<Token>();
        int index = start;
        while (tokens[index].Kind != LexerTokenKinds.FormTerminator)
        {
            expression.Add(tokens[index++]);
        }
        expression.Add(tokens[index] with { Kind = LexerTokenKinds.EndOfInput, Text = string.Empty });
        Expr value = new Parser(source, expression).ParseExpression();

        return AttributeLiteral.Normalize(value, tokens[start].Start);
    }
}
