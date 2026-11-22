namespace Erlang.Compiler;

internal static class BeamImmediateEffectClosures
{
    internal static IReadOnlyList<FunctionDefinition> Inline(IReadOnlyList<FunctionDefinition> functions)
    {
        FunctionDefinition[]? rewritten = null;
        for (int index = 0; index < functions.Count; index++)
        {
            FunctionDefinition function = functions[index];
            Clause[]? clauses = null;
            for (int position = 0; position < function.Clauses.Count; position++)
            {
                Clause original = function.Clauses[position];
                if (!LocalFunctionEntries.Walk(original.Body).Any(expression =>
                    (expression is Expr.Sequence sequence && sequence.Items.SkipLast(1).Any(item => ReadImmediateBody(item) is not null))
                    || ReadReturnedImmediateBody(expression) is not null))
                {
                    continue;
                }

                var outside = Counts(original.Body, original.Patterns);
                if (original.Guard is { } guard)
                {
                    foreach (var (name, count) in Counts(guard, []))
                    {
                        outside[name] = outside.GetValueOrDefault(name) + count;
                    }
                }

                bool changed = false;
                Expr replacement = BeamTargetedInlining.Rewrite(original.Body, Transform, rewriteComprehensions: true);
                if (changed)
                {
                    clauses ??= function.Clauses.ToArray();
                    clauses[position] = original with { Body = replacement };
                }

                Expr Transform(Expr expression)
                {
                    if (expression is Expr.Fun wrapper && ReadReturnedImmediateBody(wrapper) is { } returned
                        && CanInline(returned, isolated: true))
                    {
                        // The escaping zero-argument function owns the source
                        // identity. Only its immediate tail application is folded.
                        changed = true;

                        return wrapper with { Clauses = [wrapper.Clauses[0] with { Body = returned }] };
                    }

                    if (expression is not Expr.Sequence sequence)
                    {
                        return expression;
                    }

                    Expr[]? items = null;
                    for (int item = 0; item < sequence.Items.Count - 1; item++)
                    {
                        if (ReadImmediateBody(sequence.Items[item]) is not { } body)
                        {
                            continue;
                        }

                        if (!CanInline(body))
                        {
                            continue;
                        }

                        // Core folds an immediately invoked anonymous fun in
                        // effect position. Its local bindings cannot collide
                        // with any name in the surrounding source clause.
                        items ??= sequence.Items.ToArray();
                        items[item] = new Expr.Block(body);
                        changed = true;
                    }

                    return items is null ? expression : sequence with { Items = items };
                }

                bool CanInline(Expr body, bool isolated = false)
                {
                    Expr[] nodes = LocalFunctionEntries.Walk(body).ToArray();
                    if (nodes.Any(node => node is Expr.Fun or Expr.Receive or Expr.Try or Expr.Catch
                        or Expr.Maybe or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension))
                    {
                        return false;
                    }

                    if (isolated)
                    {
                        // A zero-argument wrapper whose whole body is this call
                        // has no sibling bindings that can observe the substitution.
                        return true;
                    }

                    var inner = Counts(body, []);

                    return nodes.SelectMany(Patterns).SelectMany(Semantics.Variables)
                        .Where(name => name != VariableScopeNames.Wildcard)
                        .All(name => outside.GetValueOrDefault(name) == inner.GetValueOrDefault(name));
                }
            }

            if (clauses is not null)
            {
                rewritten ??= functions.ToArray();
                rewritten[index] = function with { Clauses = clauses };
            }
        }

        return rewritten ?? functions;
    }

    private static Expr? ReadReturnedImmediateBody(Expr expression)
    {
        if (expression is not Expr.Fun
            {
                SelfName: null,
                Clauses: [{ Guard: null, Patterns: [], Body: var body }]
            })
        {
            return null;
        }

        while (true)
        {
            switch (body)
            {
                case Expr.Block block:
                    body = block.Body;
                    break;
                case Expr.Sequence { Items: [var item] }:
                    body = item;
                    break;
                default:
                    return ReadImmediateBody(body);
            }
        }
    }

    private static Expr? ReadImmediateBody(Expr expression) => expression is
        Expr.Apply
    {
        Arguments: [],
        Function: Expr.Fun
        {
            SelfName: null,
            Clauses: [{ Guard: null, Patterns: [], Body: var body }]
        }
    } ? body : null;

    private static Dictionary<string, int> Counts(Expr root, IEnumerable<Pattern> parameters)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string name in parameters.SelectMany(Semantics.Variables))
        {
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        foreach (Expr expression in LocalFunctionEntries.Walk(root))
        {
            if (expression is Expr.Variable variable)
            {
                counts[variable.Name] = counts.GetValueOrDefault(variable.Name) + 1;
            }

            foreach (string name in Patterns(expression).SelectMany(Semantics.Variables))
            {
                counts[name] = counts.GetValueOrDefault(name) + 1;
            }
        }

        return counts;
    }

    private static IEnumerable<Pattern> Patterns(Expr expression) => expression switch
    {
        Expr.Match match => [match.Pattern],
        Expr.MaybeMatch match => [match.Pattern],
        Expr.Case decision => decision.Clauses.SelectMany(arm => arm.Patterns),
        Expr.If decision => decision.Clauses.SelectMany(arm => arm.Patterns),
        Expr.Receive receive => receive.Clauses.SelectMany(arm => arm.Patterns),
        Expr.Try attempt => attempt.Clauses.Concat(attempt.Catches).SelectMany(arm => arm.Patterns),
        Expr.Fun function => function.Clauses.SelectMany(arm => arm.Patterns),
        Expr.Maybe maybe => maybe.Clauses.SelectMany(arm => arm.Patterns),
        Expr.ListComprehension comprehension => GeneratorPatterns(comprehension.Qualifiers),
        Expr.BinaryComprehension comprehension => GeneratorPatterns(comprehension.Qualifiers),
        Expr.MapComprehension comprehension => GeneratorPatterns(comprehension.Qualifiers),
        _ => []
    };

    private static IEnumerable<Pattern> GeneratorPatterns(IEnumerable<ComprehensionQualifier> qualifiers)
        => qualifiers.SelectMany(qualifier => qualifier switch
        {
            ComprehensionQualifier.Generator generator => new[] { generator.Pattern },
            ComprehensionQualifier.MapGenerator generator => new[] { generator.Pattern },
            ComprehensionQualifier.BinaryGenerator generator => new Pattern[] { generator.Pattern },
            ComprehensionQualifier.Zip zip => GeneratorPatterns(zip.Generators),
            _ => []
        });
}
