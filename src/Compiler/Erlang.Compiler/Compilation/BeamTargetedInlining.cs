namespace Erlang.Compiler;

internal static class BeamTargetedInlining
{
    private const int DefaultAutomaticInlineSize = 24;
    private const int AutomaticInlineEffort = 150;
    internal static (IReadOnlyList<FunctionDefinition>, IReadOnlyList<BeamSourceLocation>) Apply(
        ModuleDefinition module,
        IReadOnlyList<BeamSourceLocation> locations,
        IReadOnlySet<(string Name, int Arity)> exports
    )
    {
        var targets = new Dictionary<(string, int), (Term? Constant, int Argument, Expr.Match? ListCheck, Clause? Template)>();
        var dispatches = new Dictionary<(string, int), (FunctionDefinition Function, BeamSourceLocation Location)>();
        var failureStubs = new Dictionary<(string, int), (FunctionDefinition Function, BeamSourceLocation Location)>();
        var automaticCosts = new Dictionary<(string, int), int>();
        for (int index = 0; index < module.DeclaredFunctions.Count; index++)
        {
            var function = module.DeclaredFunctions[index];
            var signature = (function.Name, function.Arity);
            if (!module.InlineTargets.Contains(signature))
            {
                if (!module.AutomaticInlining || module.DeclaredNifs?.Contains(signature) == true
                    || function.Name == module.OnLoadFunction && function.Arity == 0
                    || function.Clauses is not [{ Guard: null } candidate]
                    || !BeamModuleCompiler.HasUnconditionalParameters(candidate))
                {
                    continue;
                }

                int limit = module.AutomaticInlineSize ?? DefaultAutomaticInlineSize;
                int cost = 0;
                foreach (Expr expression in LocalFunctionEntries.Walk(candidate.Body))
                {
                    if (++cost > limit || !IsTemplateExpression(expression))
                    {
                        cost = 0;
                        break;
                    }
                }
                if (cost == 0 || ReadListReduction(candidate) is not null)
                {
                    continue;
                }

                automaticCosts.Add(signature, cost);
            }
            if (function.Clauses is not [{ Guard: null } clause] || !BeamModuleCompiler.HasUnconditionalParameters(clause))
            {
                if (function.Clauses.All(clause => clause.Patterns.All(IsTemplatePattern))
                    && LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk).All(IsTemplateExpression))
                {
                    dispatches.Add(signature, (function, locations[index]));
                    continue;
                }

                throw Unsupported(signature);
            }

            if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term constant))
            {
                targets.Add(signature, (constant, -1, null, null));
                continue;
            }
            if (clause.Body is Expr.Variable variable)
            {
                int argument = Array.FindIndex(clause.Patterns.Cast<Pattern.Variable>().ToArray(), pattern => pattern.Name == variable.Name);
                if (argument >= 0)
                {
                    targets.Add(signature, (null, argument, null, null));
                    continue;
                }
            }
            if (ReadListReduction(clause) is { } check)
            {
                targets.Add(signature, (null, 0, check, null));
                continue;
            }

            if (LocalFunctionEntries.Walk(clause.Body).All(IsTemplateExpression))
            {
                targets.Add(signature, (null, -1, null, clause));
                continue;
            }

            throw Unsupported(signature);
        }
        if (targets.Count + dispatches.Count - automaticCosts.Count != module.InlineTargets.Count)
        {
            throw Unsupported(module.InlineTargets.First(signature =>
                !targets.ContainsKey(signature) && !dispatches.ContainsKey(signature)));
        }

        var functions = new List<FunctionDefinition>();
        var lines = new List<BeamSourceLocation>();
        int freshBinding = 0;
        for (int index = 0; index < module.DeclaredFunctions.Count; index++)
        {
            var function = module.DeclaredFunctions[index];
            var signature = (function.Name, function.Arity);
            var clauses = new List<Clause>(function.Clauses.Count);
            foreach (var clause in function.Clauses)
            {
                Clause rewritten = clause;
                if (targets.TryGetValue(signature, out var own) && own.ListCheck is { } ownCheck)
                {
                    rewritten = clause with { Body = CheckList(ownCheck, (Expr.Variable)ownCheck.Value) };
                }
                else if (clause.Body is Expr.Call { Module: null, DynamicRemote: false } call
                    && targets.TryGetValue((call.Function, call.Arguments.Count), out var target)
                    && call.Arguments.All(argument => argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _)))
                {
                    if (target.ListCheck is { } listCheck)
                    {
                        if (call.Arguments[target.Argument] is Expr.Variable input
                            && function.Clauses.Count == 1 && clause.Guard is null
                            && BeamModuleCompiler.HasUnconditionalParameters(clause))
                        {
                            rewritten = clause with { Body = CheckList(listCheck, input) };
                        }
                    }
                }

                var active = new HashSet<(string, int)> { signature };
                int remainingEffort = AutomaticInlineEffort;
                rewritten = rewritten with
                {
                    Body = Rewrite(rewritten.Body, Inline),
                    Guard = rewritten.Guard is { } guard ? Rewrite(guard, Inline) : null
                };

                Expr Inline(Expr expression)
                {
                    if (expression is not Expr.Call { Module: null, DynamicRemote: false } local)
                    {
                        return expression;
                    }

                    var targetKey = (local.Function, local.Arguments.Count);
                    bool dispatch = dispatches.TryGetValue(targetKey, out var branchTarget);
                    if ((!dispatch && !targets.ContainsKey(targetKey))
                        || automaticCosts.TryGetValue(targetKey, out int cost) && cost > remainingEffort
                        || !active.Add(targetKey))
                    {
                        return expression;
                    }

                    remainingEffort -= cost;
                    var evaluation = new List<Expr>();
                    var operands = new Expr[local.Arguments.Count];
                    for (int index = 0; index < operands.Length; index++)
                    {
                        Expr argument = local.Arguments[index];
                        if (argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _))
                        {
                            operands[index] = argument;
                            continue;
                        }

                        string temporary = FreshBinding();
                        evaluation.Add(new Expr.Match(new Pattern.Variable(temporary), argument));
                        operands[index] = new Expr.Variable(temporary);
                    }

                    Expr body;
                    if (dispatch)
                    {
                        var branches = new List<Clause>();
                        foreach (Clause branch in branchTarget.Function.Clauses)
                        {
                            var names = new Dictionary<string, Expr>(StringComparer.Ordinal);
                            Pattern[] patterns = branch.Patterns.Select(pattern => RewritePattern(pattern, names)).ToArray();
                            Expr? branchGuard = branch.Guard is { } guard ? RewriteTemplate(guard, names) : null;
                            branches.Add(new([new Pattern.Tuple(patterns)], branchGuard, RewriteTemplate(branch.Body, names)));
                        }
                        if (!branchTarget.Function.Clauses.Any(branch => branch.Guard is null
                            && BeamModuleCompiler.HasUnconditionalParameters(branch)))
                        {
                            string stub = FormattableString.Invariant($"-inlined-{local.Function}/{local.Arguments.Count}-");
                            failureStubs.TryAdd(targetKey, (new FunctionDefinition(stub, local.Arguments.Count, [])
                            {
                                IsInlineFailure = true
                            }, branchTarget.Location));
                            branches.Add(new([new Pattern.Variable(VariableScopeNames.Wildcard)], null,
                                local with { Function = stub, Arguments = operands }));
                        }
                        body = new Expr.Case(new Expr.Tuple(operands), branches);
                    }
                    else if (targets[targetKey] is { ListCheck: { } listCheck, Argument: var checkedArgument })
                    {
                        body = CheckList(listCheck, operands[checkedArgument]);
                    }
                    else if (targets[targetKey] is { Constant: { } constant })
                    {
                        body = new Expr.Literal(constant);
                    }
                    else if (targets[targetKey].Template is not { } template)
                    {
                        body = operands[targets[targetKey].Argument];
                    }
                    else
                    {
                        var arguments = new Dictionary<string, Expr>(StringComparer.Ordinal);
                        for (int index = 0; index < template.Patterns.Count; index++)
                        {
                            var parameter = (Pattern.Variable)template.Patterns[index];
                            if (parameter.Name != VariableScopeNames.Wildcard)
                            {
                                arguments.Add(parameter.Name, operands[index]);
                            }
                        }

                        body = RewriteTemplate(template.Body, arguments);
                    }

                    Expr RewriteTemplate(Expr template, Dictionary<string, Expr> arguments) =>
                        Rewrite(template, node => node switch
                        {
                            Expr.Variable variable => arguments[variable.Name],
                            Expr.Match match => match with { Pattern = RewritePattern(match.Pattern, arguments) },
                            _ => Inline(node)
                        }, pattern => RewritePattern(pattern, arguments), rewriteScope: expression => expression switch
                        {
                            Expr.Fun fun => RewriteClosure(fun, arguments),
                            Expr.ListComprehension comprehension => RewriteListComprehension(comprehension, arguments),
                            _ => null
                        });

                    Expr.ListComprehension RewriteListComprehension(
                        Expr.ListComprehension comprehension,
                        Dictionary<string, Expr> outer
                    )
                    {
                        var scope = new Dictionary<string, Expr>(outer, StringComparer.Ordinal);
                        var qualifiers = new List<ComprehensionQualifier>(comprehension.Qualifiers.Count);
                        foreach (var qualifier in comprehension.Qualifiers)
                        {
                            if (qualifier is ComprehensionQualifier.Generator generator)
                            {
                                Expr source = RewriteTemplate(generator.Source, scope);
                                foreach (string name in Semantics.Variables(generator.Pattern))
                                {
                                    scope.Remove(name);
                                }

                                qualifiers.Add(generator with { Source = source, Pattern = RewritePattern(generator.Pattern, scope) });
                            }
                            else if (qualifier is ComprehensionQualifier.Filter filter)
                            {
                                qualifiers.Add(filter with { Expression = RewriteTemplate(filter.Expression, scope) });
                            }
                            else
                            {
                                throw Unsupported(targetKey);
                            }
                        }

                        return comprehension with
                        {
                            Items = comprehension.Items.Select(item => RewriteTemplate(item, scope)).ToArray(),
                            Qualifiers = qualifiers
                        };
                    }

                    Expr.Fun RewriteClosure(Expr.Fun fun, Dictionary<string, Expr> outer)
                    {
                        string? self = fun.SelfName is null ? null : FreshBinding();
                        var clauses = new List<Clause>(fun.Clauses.Count);
                        foreach (Clause clause in fun.Clauses)
                        {
                            var scope = new Dictionary<string, Expr>(outer, StringComparer.Ordinal);
                            foreach (string parameter in clause.Patterns.SelectMany(Semantics.Variables))
                            {
                                scope.Remove(parameter);
                            }
                            if (self is not null)
                            {
                                scope[fun.SelfName!] = new Expr.Variable(self);
                            }

                            var patterns = clause.Patterns.Select(pattern => RewritePattern(pattern, scope)).ToArray();
                            Expr? guard = clause.Guard is { } condition ? RewriteTemplate(condition, scope) : null;
                            clauses.Add(clause with
                            {
                                Patterns = patterns,
                                Guard = guard,
                                Body = RewriteTemplate(clause.Body, scope)
                            });
                        }

                        return fun with
                        {
                            Clauses = clauses,
                            SelfName = self
                        };
                    }

                    Pattern RewritePattern(Pattern pattern, Dictionary<string, Expr> arguments)
                    {
                        switch (pattern)
                        {
                            case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                            case Pattern.Literal:
                                return pattern;
                            case Pattern.Variable variable:
                                if (!arguments.TryGetValue(variable.Name, out Expr? binding))
                                {
                                    binding = new Expr.Variable(FreshBinding());
                                    arguments.Add(variable.Name, binding);
                                }
                                return binding is Expr.Variable renamed
                                    ? new Pattern.Variable(renamed.Name)
                                    : AttributeLiteral.TryNormalizeConstant(binding, out Term literal)
                                        ? new Pattern.Literal(literal) : throw Unsupported(targetKey);
                            case AliasPattern alias:
                                return alias with
                                {
                                    Left = RewritePattern(alias.Left, arguments),
                                    Right = RewritePattern(alias.Right, arguments)
                                };
                            case MapPattern map:
                                Expr[] keys = map.Fields.Select(field => RewriteTemplate(field.Key, arguments)).ToArray();
                                return map with
                                {
                                    Fields = map.Fields.Select((field, index) => field with
                                    {
                                        Key = keys[index],
                                        Value = RewritePattern(field.Value, arguments)
                                    }).ToArray()
                                };
                            case BitPattern bits:
                                return bits with
                                {
                                    Segments = bits.Segments.Select(segment =>
                                    {
                                        Expr? size = segment.Specification.Size is { } expression
                                            ? RewriteTemplate(expression, arguments) : null;
                                        return segment with
                                        {
                                            Value = RewritePattern(segment.Value, arguments),
                                            Specification = segment.Specification with { Size = size }
                                        };
                                    }).ToArray()
                                };
                            case Pattern.Tuple tuple:
                                return tuple with { Items = tuple.Items.Select(item => RewritePattern(item, arguments)).ToArray() };
                            case Pattern.List list:
                                return list with
                                {
                                    Items = list.Items.Select(item => RewritePattern(item, arguments)).ToArray(),
                                    Tail = list.Tail is null ? null : RewritePattern(list.Tail, arguments)
                                };
                            default:
                                throw Unsupported(targetKey);
                        }
                    }
                    active.Remove(targetKey);

                    return evaluation.Count == 0 ? body : new Expr.Sequence([.. evaluation, body]);
                }

                string FreshBinding() => FormattableString.Invariant($"$inline{freshBinding++}");
                clauses.Add(rewritten);
            }
            functions.Add(function with { Clauses = clauses });
            lines.Add(locations[index]);
        }

        foreach (var (stub, location) in failureStubs.Values.OrderBy(entry => entry.Function.Name, StringComparer.Ordinal))
        {
            functions.Add(stub);
            lines.Add(location);
        }

        var retained = new bool[functions.Count];
        var pending = new Queue<int>();
        var indices = new Dictionary<(string, int), int>();
        for (int index = 0; index < functions.Count; index++)
        {
            FunctionDefinition function = functions[index];
            var signature = (function.Name, function.Arity);
            indices.Add(signature, index);
            if ((!function.IsInlineFailure && !targets.ContainsKey(signature) && !dispatches.ContainsKey(signature))
                || exports.Contains(signature) || function.Arity == 0 && function.Name == module.OnLoadFunction)
            {
                retained[index] = true;
                pending.Enqueue(index);
            }
        }

        while (pending.TryDequeue(out int caller))
        {
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(functions[caller].Clauses)
                .SelectMany(LocalFunctionEntries.Walk))
            {
                var reference = expression switch
                {
                    Expr.Call { Module: null, DynamicRemote: false } local => (local.Function, local.Arguments.Count),
                    Expr.FunctionReference local => (local.Name, local.Arity),
                    _ => default
                };
                if (reference.Item1 is not null && indices.TryGetValue(reference, out int target) && !retained[target])
                {
                    retained[target] = true;
                    pending.Enqueue(target);
                }
            }
        }

        return (functions.Where((_, index) => retained[index]).ToArray(),
            lines.Where((_, index) => retained[index]).ToArray());
    }

    private static bool IsTemplateExpression(Expr expression) =>
        expression is Expr.Literal or Expr.Variable or Expr.Tuple or Expr.List or Expr.Map or Expr.Bits
            or Expr.Call or Expr.Apply or Expr.Unary or Expr.Binary or Expr.Block or Expr.Sequence
            or Expr.RecordConstruction or Expr.RecordUpdate or Expr.RecordAccess or Expr.RecordTest or Expr.FunctionReference
        || expression is Expr.Match match && IsTemplatePattern(match.Pattern)
        || expression is Expr.Case decision && decision.Clauses.All(clause => clause.Patterns.All(IsTemplatePattern))
        || expression is Expr.Receive receive && HasTemplatePatterns(receive.Clauses)
        || expression is Expr.Try attempt && HasTemplatePatterns(attempt.Clauses) && HasTemplatePatterns(attempt.Catches)
        || expression is Expr.Fun fun && fun.Clauses.All(clause => clause.Patterns.All(IsScopedTemplatePattern))
        || expression is Expr.ListComprehension comprehension
            && comprehension.Qualifiers.All(qualifier => qualifier is ComprehensionQualifier.Filter
                || qualifier is ComprehensionQualifier.Generator generator && IsScopedTemplatePattern(generator.Pattern))
        || expression is Expr.Catch or Expr.If or Expr.GuardAlternatives;

    private static bool IsScopedTemplatePattern(Pattern pattern) => pattern switch
    {
        Pattern.Variable or Pattern.Literal => true,
        AliasPattern alias => IsScopedTemplatePattern(alias.Left) && IsScopedTemplatePattern(alias.Right),
        Pattern.Tuple tuple => tuple.Items.All(IsScopedTemplatePattern),
        Pattern.List list => list.Items.All(IsScopedTemplatePattern)
            && (list.Tail is null || IsScopedTemplatePattern(list.Tail)),
        _ => false
    };

    private static bool HasTemplatePatterns(IEnumerable<Clause> clauses) =>
        clauses.All(clause => clause.Patterns.All(IsTemplatePattern));

    private static bool IsTemplatePattern(Pattern pattern) => pattern switch
    {
        Pattern.Variable or Pattern.Literal => true,
        AliasPattern alias => IsTemplatePattern(alias.Left) && IsTemplatePattern(alias.Right),
        MapPattern map => map.Fields.All(field => IsTemplatePattern(field.Value)
            && LocalFunctionEntries.Walk(field.Key).All(IsTemplateExpression)),
        BitPattern bits => bits.Segments.All(segment => IsTemplatePattern(segment.Value)
            && (segment.Specification.Size is null || LocalFunctionEntries.Walk(segment.Specification.Size).All(IsTemplateExpression))),
        Pattern.Tuple tuple => tuple.Items.All(IsTemplatePattern),
        Pattern.List list => list.Items.All(IsTemplatePattern)
            && (list.Tail is null || IsTemplatePattern(list.Tail)),
        _ => false
    };

    internal static Expr Rewrite(
        Expr expression,
        Func<Expr, Expr> transform,
        Func<Pattern, Pattern>? patternTransform = null,
        bool rewriteComprehensions = false,
        Func<Expr, Expr?>? rewriteScope = null
    )
    {
        if (rewriteScope?.Invoke(expression) is { } scoped)
        {
            return scoped;
        }

        Expr Visit(Expr child) => Rewrite(child, transform, patternTransform, rewriteComprehensions, rewriteScope);
        Expr? Optional(Expr? child) => child is null ? null : Visit(child);
        Expr[] Items(IEnumerable<Expr> children) => children.Select(Visit).ToArray();
        ComprehensionQualifier Qualifier(ComprehensionQualifier qualifier) => qualifier switch
        {
            ComprehensionQualifier.Generator item => item with { Source = Visit(item.Source) },
            ComprehensionQualifier.MapGenerator item => item with { Source = Visit(item.Source) },
            ComprehensionQualifier.BinaryGenerator item => item with { Source = Visit(item.Source) },
            ComprehensionQualifier.Filter item => item with { Expression = Visit(item.Expression) },
            ComprehensionQualifier.Zip item => item with { Generators = item.Generators.Select(Qualifier).ToArray() },
            _ => qualifier
        };
        Clause[] Clauses(IEnumerable<Clause> clauses) => clauses.Select(clause =>
            clause with
            {
                Patterns = patternTransform is null ? clause.Patterns : clause.Patterns.Select(patternTransform).ToArray(),
                Guard = Optional(clause.Guard),
                Body = Visit(clause.Body)
            }).ToArray();

        return transform(expression switch
        {
            Expr.Tuple tuple => tuple with { Items = Items(tuple.Items) },
            Expr.List list => list with { Items = Items(list.Items), Tail = Optional(list.Tail) },
            Expr.ListComprehension list when rewriteComprehensions => list with
            {
                Items = Items(list.Items), Qualifiers = list.Qualifiers.Select(Qualifier).ToArray()
            },
            Expr.BinaryComprehension bits when rewriteComprehensions => bits with
            {
                Body = Visit(bits.Body), Qualifiers = bits.Qualifiers.Select(Qualifier).ToArray()
            },
            Expr.MapComprehension map when rewriteComprehensions => map with
            {
                Fields = map.Fields.Select(field => field with { Key = Visit(field.Key), Value = Visit(field.Value) }).ToArray(),
                Qualifiers = map.Qualifiers.Select(Qualifier).ToArray()
            },
            Expr.Map map => map with
            {
                Base = Optional(map.Base),
                Fields = map.Fields.Select(field => field with { Key = Visit(field.Key), Value = Visit(field.Value) }).ToArray()
            },
            Expr.Bits bits => bits with
            {
                Segments = bits.Segments.Select(segment =>
                    segment with { Value = Visit(segment.Value), Size = Optional(segment.Size) }).ToArray()
            },
            Expr.Call call => call with { Arguments = Items(call.Arguments) },
            Expr.Apply apply => apply with { Function = Visit(apply.Function), Arguments = Items(apply.Arguments) },
            Expr.Unary unary => unary with { Operand = Visit(unary.Operand) },
            Expr.Binary binary => binary with { Left = Visit(binary.Left), Right = Visit(binary.Right) },
            Expr.RecordConstruction record => record with
            {
                Fields = record.Fields.Select(field => field with { Value = Visit(field.Value) }).ToArray(),
                Wildcard = Optional(record.Wildcard)
            },
            Expr.RecordUpdate record => record with
            {
                Value = Visit(record.Value),
                Fields = record.Fields.Select(field => field with { Value = Visit(field.Value) }).ToArray()
            },
            Expr.RecordAccess record => record with { Value = Visit(record.Value) },
            Expr.RecordTest record => record with { Value = Visit(record.Value) },
            Expr.Block block => block with { Body = Visit(block.Body) },
            Expr.Sequence sequence => sequence with { Items = Items(sequence.Items) },
            Expr.Match match => match with { Value = Visit(match.Value) },
            Expr.MaybeMatch match => match with { Value = Visit(match.Value) },
            Expr.Maybe maybe => maybe with { Items = Items(maybe.Items), Clauses = Clauses(maybe.Clauses) },
            Expr.Case decision => decision with { Value = Visit(decision.Value), Clauses = Clauses(decision.Clauses) },
            Expr.If decision => decision with { Clauses = Clauses(decision.Clauses) },
            Expr.Receive receive => receive with
            {
                Clauses = Clauses(receive.Clauses), Timeout = Optional(receive.Timeout), After = Optional(receive.After)
            },
            Expr.Try attempt => attempt with
            {
                Body = Visit(attempt.Body), Clauses = Clauses(attempt.Clauses),
                Catches = Clauses(attempt.Catches), After = Optional(attempt.After)
            },
            Expr.Catch attempt => attempt with { Operand = Visit(attempt.Operand) },
            Expr.Fun fun => fun with { Clauses = Clauses(fun.Clauses) },
            Expr.GuardAlternatives alternatives => alternatives with { Items = Items(alternatives.Items) },
            _ => expression
        });
    }

    private static Expr.Match? ReadListReduction(Clause clause)
    {
        if (clause is not
            {
                Patterns: [Pattern.Variable parameter],
                Body: Expr.Sequence sequence
            }
            || sequence.Items is not [
                    Expr.Match
            {
                Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                Value: Expr.Variable input
            } match,
                    Expr.List { Items: [var returnedHead], Tail: Expr.Variable returnedTail }
                ]
            || input.Name != parameter.Name || returnedTail.Name != tail.Name
            || head.Name == tail.Name || head.Name == input.Name || tail.Name == input.Name
            || (returnedHead is Expr.Block block ? block.Body : returnedHead) is not Expr.Variable item
            || item.Name != head.Name)
        {
            return null;
        }

        return match;
    }

    private static Expr.Sequence CheckList(Expr.Match match, Expr input)
    {
        var discard = new Pattern.Variable(VariableScopeNames.Wildcard);
        var check = match with { Pattern = new Pattern.List([discard], discard), Value = input };

        return new([check, input]);
    }

    private static NotSupportedException Unsupported((string Name, int Arity) target) =>
        new(BeamEmissionFormat.UnsupportedInlineTarget(target.Name, target.Arity));
}
