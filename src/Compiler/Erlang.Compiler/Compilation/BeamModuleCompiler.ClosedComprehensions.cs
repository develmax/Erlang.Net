namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (IReadOnlyList<FunctionDefinition>, IReadOnlyList<BeamSourceLocation>) ExtractClosedComprehensions(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyList<BeamSourceLocation> locations
    )
    {
        var rewritten = functions.ToArray();
        var helpers = new List<(FunctionDefinition Function, BeamSourceLocation Location)>();
        var declared = functions.Select(function => (function.Name, function.Arity)).ToHashSet();
        for (int index = 0; index < functions.Count; index++)
        {
            var function = functions[index];
            if (function.Clauses is [var filteredClause]
                && ExtractCapturedListFilter(function, filteredClause, locations[index], declared) is { } capturedFilter)
            {
                rewritten[index] = function with { Clauses = [filteredClause with { Body = capturedFilter.Body }] };
                helpers.Add((capturedFilter.Helper, capturedFilter.Location));
                continue;
            }

            if (function.Clauses is [var projectionClause])
            {
                string projectionName = MappingName(function.Name, function.Arity);
                if (!declared.Contains((projectionName, 1))
                    && ExtractTupleProjection(projectionClause.Body, projectionName, locations[index]) is { } projection)
                {
                    declared.Add((projectionName, 1));
                    rewritten[index] = function with { Clauses = [projectionClause with { Body = projection.Body }] };
                    helpers.Add((projection.Helper, projection.Location));
                    if (projection.Body is not Expr.Call { Module: null } call || call.Function != projectionName)
                    {
                        deferredProjections.Add(projectionName);
                    }
                    continue;
                }
            }
            if (ReadTupleKeyFilter(function) is { } filtered)
            {
                string helperName = MappingName(function.Name, function.Arity);
                if (!declared.Add((helperName, 1)))
                {
                    continue;
                }

                var original = function.Clauses[0];
                rewritten[index] = function with
                {
                    Clauses = [original with { Body = new Expr.Call(null, helperName, [filtered.Source]) }]
                };
                var filterHelper = new FunctionDefinition(helperName, 1, [original]);
                var helperLocation = locations[index];
                if (filtered.Comprehension.Source is { } filterLocation)
                {
                    int line = filtered.Comprehension.PreprocessorLine == 0
                        ? filterLocation.Line : filtered.Comprehension.PreprocessorLine;
                    helperLocation = new(filtered.Comprehension.PreprocessorFile ?? helperLocation.File, line);
                }
                helpers.Add((filterHelper, helperLocation));
                continue;
            }
            if (function.Clauses is [var commandClause]
                && commandClause.Body is Expr.Sequence sequence
                && (ExtractWxUnicodeMapping(function, sequence, locations[index], declared)
                    ?? ExtractWxOptionMapping(function, sequence, locations[index], declared)) is { } unicode)
            {
                rewritten[index] = function with { Clauses = [commandClause with { Body = unicode.Body }] };
                helpers.Add((unicode.Helper, unicode.Location));
                deferredProjections.Add(unicode.Helper.Name);
                wxUnicodeMappings.Add(unicode.Helper.Name);
                continue;
            }

            if (function.Clauses is [var fieldClause]
                && ExtractListComprehensionHelper(function, fieldClause, locations[index], declared) is { } field)
            {
                rewritten[index] = function with { Clauses = [fieldClause with { Body = field.Body }] };
                helpers.Add((field.Helper, field.Location));
                continue;
            }

            if (function.Clauses is [var capturedClause]
                && ExtractCapturedComprehension(function, capturedClause, locations[index], declared) is { } captured)
            {
                rewritten[index] = function with { Clauses = [capturedClause with { Body = captured.Body }] };
                helpers.Add((captured.Helper, captured.Location));
                continue;
            }

            if (function.Clauses is not [var clause]
                || clause.Body is not Expr.Sequence { Items: [Expr.Match binding, Expr.ListComprehension comprehension] }
                || comprehension is not
                {
                    Items: [var body],
                    Qualifiers: [ComprehensionQualifier.Generator
                    {
                        Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                    } generator]
                }
                || binding.Pattern is not Pattern.Variable bound || bound.Name != source.Name
                || item.Name == VariableScopeNames.Wildcard || !ClosedMappingExpression(body, item.Name))
            {
                continue;
            }
            Expr mapped = body is Expr.Block block ? block.Body : body;
            if (mapped is not Expr.Call)
            {
                continue;
            }

            string name = MappingName(function.Name, function.Arity);
            if (!declared.Add((name, 1)))
            {
                continue;
            }
            if (item.Name == "BeamComprehensionTail")
            {
                continue;
            }

            var helper = BuildClosedMapping(name, item, mapped);
            rewritten[index] = function with
            {
                Clauses = [clause with { Body = new Expr.Sequence([binding, new Expr.Call(null, name, [source])]) }]
            };
            var location = locations[index];
            if (ReadMappingConversion(mapped) is not null && generator.GeneratorLine != 0)
            {
                location = new(generator.GeneratorFile ?? location.File, generator.GeneratorLine);
            }
            else if (comprehension.Source is { } sourceLocation)
            {
                int line = comprehension.PreprocessorLine == 0 ? sourceLocation.Line : comprehension.PreprocessorLine;
                location = new(comprehension.PreprocessorFile ?? location.File, line);
            }
            helpers.Add((helper, location));
        }

        helpers.Reverse();
        return helpers.Count == 0
            ? (functions, locations)
            : ([.. rewritten, .. helpers.Select(helper => helper.Function)], [.. locations, .. helpers.Select(helper => helper.Location)]);
    }

    private static (Expr.Sequence Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractWxUnicodeMapping(
        FunctionDefinition function,
        Expr.Sequence sequence,
        BeamSourceLocation location,
        HashSet<(string, int)> declared,
        bool option = false
    )
    {
        int index = 0;
        foreach (Expr expression in sequence.Items)
        {
            if (expression is Expr.Match
                {
                    Pattern: Pattern.Variable bound,
                    Value: Expr.ListComprehension
                    {
                        Items: [Expr.Call
                        {
                            Module: "unicode", Function: "characters_to_binary", DynamicRemote: false,
                            Arguments: [var input]
                        } mapped],
                        Qualifiers: [ComprehensionQualifier.Generator
                        {
                            Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                        } generator]
                    }
                } binding
                && ReadUnicodeMappingInput(input) is { } mappedInput && item.Name == mappedInput.Name
                && item.Name != VariableScopeNames.Wildcard
                && (option
                    ? sequence.Items.Count == 2 && sequence.Items[^1] is Expr.Tuple
                        { Items: [Expr.Literal { Value: Atom }, Expr.Variable result] } && result.Name == bound.Name
                    : sequence.Items.Skip(index + 1).OfType<Expr.Call>().Any(call => IsMappingConsumer(call, bound.Name))))
            {
                string helperName = MappingName(function.Name, function.Arity);
                const string tail = "BeamComprehensionTail";
                if (item.Name == tail || !declared.Add((helperName, 1)))
                {
                    return null;
                }

                var helperLocation = generator.GeneratorLine == 0 ? location
                    : new BeamSourceLocation(generator.GeneratorFile ?? location.File, generator.GeneratorLine);
                var call = new Expr.Call(null, helperName, [source])
                {
                    SourceLine = helperLocation.Line,
                    PreprocessorFile = helperLocation.File
                };
                Expr[] items = sequence.Items.ToArray();
                items[index] = binding with { Value = call };
                return (sequence with { Items = items }, BuildClosedMapping(helperName, item, mapped), helperLocation);
            }

            index++;
        }

        return null;

        bool IsMappingConsumer(Expr.Call call, string name)
        {
            if (call is { Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false })
            {
                return UsesBinding(call.Arguments, name);
            }

            return call is { DynamicRemote: true, Arguments: [
                Expr.Variable module, Expr.Literal { Value: Atom { Name: "queue_cmd" } },
                Expr.List { Tail: null } operands
            ] } && UsesBinding(operands.Items, name) && sequence.Items.Take(index).Any(item => item is Expr.Match
            {
                Pattern: Pattern.Variable binding,
                Value: Expr.Literal { Value: Atom { Name: "wxe_util" } }
            } && binding.Name == module.Name);
        }

        static bool UsesBinding(IReadOnlyList<Expr> operands, string name) =>
            operands.OfType<Expr.Variable>().Any(argument => argument.Name == name);
    }

    private static (string Name, Term? Suffix)? ReadUnicodeMappingInput(Expr input)
    {
        if (input is Expr.Variable variable)
        {
            return (variable.Name, null);
        }

        return input is Expr.List { Items: [Expr.Variable first, ..] } list
            && AttributeLiteral.TryNormalizeConstant(new Expr.List(list.Items.Skip(1).ToArray(), list.Tail), out Term suffix)
            && suffix is Cons { Head: Integer zero, Tail: Nil } && zero.Value.IsZero
            ? (first.Name, suffix) : null;
    }

    private static (Expr.Sequence Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractWxOptionMapping(
        FunctionDefinition function,
        Expr.Sequence sequence,
        BeamSourceLocation location,
        HashSet<(string, int)> declared
    )
    {
        if (!sequence.Items.OfType<Expr.Call>().Any(call => call.Module == "wxe_util" && call.Function == "queue_cmd"))
        {
            return null;
        }

        for (int index = 0; index < sequence.Items.Count; index++)
        {
            if (sequence.Items[index] is not Expr.Match { Value: Expr.Fun lambda } binding)
            {
                continue;
            }

            for (int branch = 0; branch < lambda.Clauses.Count; branch++)
            {
                Clause clause = lambda.Clauses[branch];
                if (clause.Body is not Expr.Sequence body
                    || ExtractWxUnicodeMapping(function, body, location, declared, option: true) is not { } mapping)
                {
                    continue;
                }

                Clause[] clauses = lambda.Clauses.ToArray();
                clauses[branch] = clause with { Body = mapping.Body };
                Expr[] items = sequence.Items.ToArray();
                items[index] = binding with { Value = lambda with { Clauses = clauses } };
                return (sequence with { Items = items }, mapping.Helper, mapping.Location);
            }
        }

        return null;
    }

    private static FunctionDefinition BuildClosedMapping(
        string name,
        Pattern.Variable item,
        Expr mapped,
        Pattern.Variable? capture = null
    )
    {
        string tail = capture is null ? "BeamComprehensionTail" : "@BeamComprehensionTail";
        string bad = capture is null ? "BeamComprehensionInput" : "@BeamComprehensionInput";
        Pattern[] capturedPatterns = capture is null ? [] : [capture];
        Pattern[] ignoredCaptures = capture is null ? [] : [new Pattern.Variable(VariableScopeNames.Wildcard)];
        Expr[] capturedArguments = capture is null ? [] : [new Expr.Variable(capture.Name)];
        int arity = capturedArguments.Length + 1;
        return new FunctionDefinition(name, arity,
        [
            new([new Pattern.List([item], new Pattern.Variable(tail)), .. capturedPatterns], null,
                new Expr.List([mapped], new Expr.Call(null, name, [new Expr.Variable(tail), .. capturedArguments]))),
            new([new Pattern.List([], null), .. ignoredCaptures], null, new Expr.Literal(Nil.Value)),
            new([new Pattern.Variable(bad), .. ignoredCaptures], null, new Expr.Call(ErlangModuleNames.Module, "error",
                [new Expr.Tuple([new Expr.Literal(Term.A(ErlangErrorReasons.BadGenerator)), new Expr.Variable(bad)])]))
        ]);
    }

    private static bool ClosedMappingExpression(Expr expression, string item) => expression switch
    {
        Expr.Variable variable => variable.Name == item,
        Expr.Block block => ClosedMappingExpression(block.Body, item),
        Expr.Call { DynamicRemote: false } call => call.Arguments.All(argument => ClosedMappingExpression(argument, item)),
        _ => AttributeLiteral.TryNormalizeConstant(expression, out _)
    };
}
