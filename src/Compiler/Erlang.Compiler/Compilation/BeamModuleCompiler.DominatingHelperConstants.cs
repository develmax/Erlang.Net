namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static Dictionary<string, Term> ReadDominatingHelperConstants(
        Clause clause,
        Expr helper,
        bool allowListComprehensionPrefix = false,
        bool allowFixedListComprehensionPrefix = false
    )
    {
        var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
        if (clause.Body is not Expr.Sequence sequence)
        {
            return constants;
        }

        var bound = clause.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
        foreach (Expr statement in sequence.Items)
        {
            if (ReferenceEquals(ReadDirectHelperExpression(statement), helper))
            {
                return constants;
            }

            if (statement is Expr.Match { Pattern: Pattern.Variable variable } binding)
            {
                if (!IsNonbindingPrefix(binding.Value))
                {
                    break;
                }

                // A match against an already-bound name is a check, not a new value.
                if (variable.Name != VariableScopeNames.Wildcard && bound.Add(variable.Name)
                    && AttributeLiteral.TryNormalizeConstant(binding.Value, out Term value, constants))
                {
                    constants.Add(variable.Name, value);
                }
            }
            else if (allowFixedListComprehensionPrefix
                && statement is Expr.Match { Pattern: Pattern.List results, Value: Expr.ListComprehension previous }
                && IsFixedListComprehensionBinding(clause, previous))
            {
                // Only the outer result match binds names in this clause.
                bound.UnionWith(Semantics.Variables(results));
            }
            else if (!IsNonbindingPrefix(statement))
            {
                break;
            }
        }

        constants.Clear();

        return constants;

        // Comprehension scopes do not export their internal bindings into the statement sequence.
        bool IsNonbindingPrefix(Expr value) => allowListComprehensionPrefix && value is Expr.ListComprehension
            || LocalFunctionEntries.Walk(value).All(IsNonbindingHelperPrefixValue);
    }

    private static Expr ReadDirectHelperExpression(Expr statement) => statement switch
    {
        Expr.Match match => match.Value,
        Expr.Case decision => decision.Value,
        _ => statement
    };

    private static bool IsNonbindingHelperPrefixValue(Expr value) => value is
        Expr.Literal or Expr.Variable or Expr.Tuple or Expr.List or Expr.Map or Expr.Bits
        or Expr.Unary or Expr.Binary or Expr.Call or Expr.Apply or Expr.FunctionReference
        or Expr.RecordConstruction or Expr.RecordAccess or Expr.RecordTest or Expr.RecordUpdate;

    private static Expr SubstituteHelperOperandConstants(Expr expression, IReadOnlyDictionary<string, Term> constants) =>
        BeamTargetedInlining.Rewrite(
            expression,
            item => item is Expr.Variable variable && constants.TryGetValue(variable.Name, out Term? value)
                ? new Expr.Literal(value) : item
        );

    private static FunctionDefinition SubstituteDirectAnonymousHelperConstants(FunctionDefinition function)
    {
        Clause[]? rewritten = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Sequence sequence)
            {
                continue;
            }

            Expr[]? statements = null;
            for (int position = 1; position < sequence.Items.Count; position++)
            {
                Expr statement = sequence.Items[position];
                if (ReadDirectHelperExpression(statement) is not Expr.Fun { SelfName: null } helper
                    || SubstituteAnonymousHelper(clause, helper) is not { } replacement)
                {
                    continue;
                }

                statements ??= sequence.Items.ToArray();
                statements[position] = statement switch
                {
                    Expr.Match match => match with { Value = replacement },
                    Expr.Case decision => decision with { Value = replacement },
                    _ => replacement
                };
            }

            if (statements is not null)
            {
                rewritten ??= function.Clauses.ToArray();
                rewritten[index] = clause with { Body = sequence with { Items = statements } };
            }
        }

        return rewritten is null ? function : function with { Clauses = rewritten };
    }

    private static Expr.Fun? SubstituteAnonymousHelper(Clause owner, Expr.Fun helper)
    {
        var constants = ReadDominatingHelperConstants(owner, helper);
        if (constants.Count == 0)
        {
            return null;
        }

        Expr[] scope = LocalFunctionEntries.ClauseExpressions(helper.Clauses).SelectMany(LocalFunctionEntries.Walk).ToArray();
        if (scope.Any(item => item is Expr.Fun or Expr.Try or Expr.Receive or Expr.Maybe or Expr.MaybeMatch
            or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension))
        {
            return null;
        }

        var patterns = helper.Clauses.SelectMany(clause => clause.Patterns).Concat(scope.SelectMany(item => item switch
        {
            Expr.Match match => [match.Pattern],
            Expr.Case decision => decision.Clauses.SelectMany(clause => clause.Patterns),
            Expr.If conditional => conditional.Clauses.SelectMany(clause => clause.Patterns),
            _ => Enumerable.Empty<Pattern>()
        })).ToArray();
        // Preserve shadowing, equality against an existing binding, and reads in map keys and bit sizes.
        foreach (string name in patterns.SelectMany(Semantics.Variables))
        {
            constants.Remove(name);
        }

        foreach (Expr.Variable read in LocalFunctionEntries.ClauseExpressions([new Clause(patterns, null, new Expr.Literal(Nil.Value))])
            .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>())
        {
            constants.Remove(read.Name);
        }

        foreach (var apply in scope.OfType<Expr.Apply>())
        {
            // Keep the existing runtime apply semantics for all variable callees.
            foreach (Expr.Variable read in LocalFunctionEntries.Walk(apply.Function).OfType<Expr.Variable>())
            {
                constants.Remove(read.Name);
            }
        }

        if (constants.Count == 0 || !scope.OfType<Expr.Variable>().Any(read => constants.ContainsKey(read.Name)))
        {
            return null;
        }

        return helper with
        {
            Clauses = helper.Clauses.Select(clause => clause with
            {
                Guard = clause.Guard is null ? null : SubstituteHelperOperandConstants(clause.Guard, constants),
                Body = SubstituteHelperOperandConstants(clause.Body, constants)
            }).ToArray()
        };
    }
}
