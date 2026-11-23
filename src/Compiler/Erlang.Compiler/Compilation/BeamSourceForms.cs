namespace Erlang.Compiler;

internal static class BeamSourceForms
{
    internal static IReadOnlyList<BeamSourceLocation> FunctionLines(IReadOnlyList<Token> tokens, ModuleDefinition module)
    {
        var lines = new List<BeamSourceLocation>();
        var attributes = module.Attributes.Select(attribute => attribute.Name).ToHashSet(StringComparer.Ordinal);
        bool seenModule = false;

        foreach (int index in Starts(tokens))
        {
            var token = tokens[index];
            if (token.Kind == LexerTokenKinds.Symbol && token.Text == ErlangSyntaxTokens.AttributeIntroducer)
            {
                switch (tokens[index + 1].Text)
                {
                    case ModuleAttributeNames.Module when !seenModule:
                        seenModule = true;
                        continue;
                    case "spec" or "callback" or "optional_callbacks" when seenModule:
                    case string type when seenModule && Parser.IsTypeDefinition(type):
                    case ModuleAttributeNames.Export when seenModule:
                    case ModuleAttributeNames.ExportType when seenModule:
                    case SourceCallBindingNames.Import when seenModule:
                    case ModuleAttributeNames.Record when seenModule:
                    case ModuleAttributeNames.Nifs when seenModule:
                    case ModuleAttributeNames.OnLoad when seenModule:
                    case ModuleAttributeNames.Compile when seenModule && !module.HasOtherCompileOptions:
                    case ModuleAttributeNames.Doc when seenModule:
                    case ModuleAttributeNames.ModuleDoc when seenModule:
                        continue;
                    case string name when seenModule && attributes.Contains(name):
                        continue;
                    case string name:
                        throw new NotSupportedException(BeamEmissionFormat.UnsupportedSourceAttribute(name));
                }
            }

            if (!seenModule)
            {
                throw new NotSupportedException(BeamEmissionFormat.Unsupported);
            }

            lines.Add(new(token.PreprocessorFile, token.PreprocessorLine));
        }

        if (module.DiameterAccessorLines.Count > 0)
        {
            // diameter_exprecs inserts its forms before the first original function.
            // The executable entry inherits that file and the generated function's first clause line.
            string? generatedFile = lines.Count > 0 ? lines[0].File : null;
            lines.InsertRange(0, module.DiameterAccessorLines.Select(line => new BeamSourceLocation(generatedFile, line + 1)));
        }

        if (module.HasGeneratedEunitTest)
        {
            lines.Add(default);
        }

        return lines;
    }

    internal static IEnumerable<int> Starts(IReadOnlyList<Token> tokens)
    {
        for (int index = 0; index < tokens.Count - 1; index++)
        {
            if (index == 0 || tokens[index - 1].Kind == LexerTokenKinds.FormTerminator)
            {
                yield return index;
            }
        }
    }
}
