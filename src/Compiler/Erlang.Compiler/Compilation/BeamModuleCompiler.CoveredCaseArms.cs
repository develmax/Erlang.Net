namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition FoldCoveredCaseArms(FunctionDefinition function)
    {
        FunctionDefinition? localTail = SsaTypeInferenceEnabled ? function : null;
        Clause[]? rewritten = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            Expr body = FoldCoveredTerminalCases(clause.Body, localTail);
            if (!ReferenceEquals(body, clause.Body))
            {
                rewritten ??= function.Clauses.ToArray();
                rewritten[index] = clause with { Body = body };
            }
        }

        return rewritten is null ? function : function with { Clauses = rewritten };
    }

    private static Expr FoldCoveredTerminalCases(Expr expression, FunctionDefinition? localTail)
    {
        if (expression is Expr.Sequence { Items.Count: > 0 } sequence)
        {
            Expr last = FoldCoveredTerminalCases(sequence.Items[^1], localTail);
            if (ReferenceEquals(last, sequence.Items[^1]))
            {
                return sequence;
            }

            Expr[] items = sequence.Items.ToArray();
            items[^1] = last;

            return sequence with { Items = items };
        }

        if (expression is not Expr.Case decision)
        {
            return expression;
        }

        Clause[]? rewritten = null;
        for (int index = 0; index < decision.Clauses.Count; index++)
        {
            Clause clause = decision.Clauses[index];
            Expr body = FoldCoveredTerminalCases(clause.Body, localTail);
            if (!ReferenceEquals(body, clause.Body))
            {
                rewritten ??= decision.Clauses.ToArray();
                rewritten[index] = clause with { Body = body };
            }
        }

        IReadOnlyList<Clause> clauses = rewritten ?? decision.Clauses;
        Expr.Call? commonTail = localTail is not null ? ReadClosedLiteralCaseTail(clauses, localTail) : null;
        List<Clause>? kept = null;
        for (int index = 0; index < clauses.Count; index++)
        {
            Clause clause = clauses[index];
            if (index + 1 < clauses.Count
                && (IsCoveredCaseArm(clause, clauses[index + 1]) || RepeatsWildcardTail(clause, commonTail)))
            {
                kept ??= clauses.Take(index).ToList();
            }
            else
            {
                kept?.Add(clause);
            }
        }

        return kept is not null ? decision with { Clauses = kept }
            : rewritten is not null ? decision with { Clauses = rewritten } : decision;
    }

    private static Expr.Call? ReadClosedLiteralCaseTail(IReadOnlyList<Clause> clauses, FunctionDefinition enclosing)
    {
        if (clauses.Count < 2
            || clauses[^1] is not
            {
                Patterns: [Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                Guard: null,
                Body: Expr.Call
                {
                    Module: null,
                    DynamicRemote: false
                } fallback
            }
            || fallback.Function != enclosing.Name
            || fallback.Arguments.Count != enclosing.Arity)
        {
            return null;
        }

        // As in beam_ssa_share, a literal successor equivalent to the default
        // can be dropped. Exact, distinct keys rule out exposing a later arm.
        var keys = new HashSet<Term>();
        for (int index = 0; index < clauses.Count - 1; index++)
        {
            if (clauses[index].Patterns is not [Pattern.Literal key] || !keys.Add(key.Value))
            {
                return null;
            }
        }

        return fallback;
    }

    private static bool RepeatsWildcardTail(Clause clause, Expr.Call? fallback) =>
        fallback is not null
        && clause is { Guard: null, Body: Expr.Call { Module: null, DynamicRemote: false } call }
        && call.Function == fallback.Function
        && call.Arguments.Count == fallback.Arguments.Count
        && call.Arguments.Zip(fallback.Arguments, SamePureTailArgument).All(equal => equal);

    private static bool SamePureTailArgument(Expr earlier, Expr later)
    {
        if (earlier is Expr.List left && later is Expr.List right)
        {
            return left.Items.Count == right.Items.Count
                && left.Items.Zip(right.Items, SamePureCaseResult).All(equal => equal)
                && (left.Tail is null && right.Tail is null
                    || left.Tail is { } leftTail && right.Tail is { } rightTail && SamePureCaseResult(leftTail, rightTail));
        }

        return SamePureCaseResult(earlier, later);
    }

    private static bool IsCoveredCaseArm(Clause earlier, Clause later)
    {
        return earlier.Guard is null && later.Guard is null
            && earlier.Patterns is [Pattern.Tuple specific] && later.Patterns is [Pattern.Tuple covering]
            && CoversWithoutChangingBindings(specific, covering)
            && SamePureCaseResult(earlier.Body, later.Body);
    }

    private static bool CoversWithoutChangingBindings(Pattern specific, Pattern covering)
    {
        // A wildcard can drop structural tests. Any binding or equality constraint
        // associated with a variable must still occur at the same path in both arms.
        return (specific, covering) switch
        {
            (_, Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }) => HasNoCasePatternBindings(specific),
            (Pattern.Variable left, Pattern.Variable right) => left.Name == right.Name,
            (Pattern.Literal left, Pattern.Literal right) => left.Value.Equals(right.Value),
            (Pattern.Tuple left, Pattern.Tuple right) => left.Items.Count == right.Items.Count
                && left.Items.Zip(right.Items, CoversWithoutChangingBindings).All(covered => covered),
            _ => false
        };
    }

    private static bool HasNoCasePatternBindings(Pattern pattern) => pattern switch
    {
        Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard } or Pattern.Literal => true,
        Pattern.Tuple tuple => tuple.Items.All(HasNoCasePatternBindings),
        _ => false
    };

    private static bool SamePureCaseResult(Expr earlier, Expr later)
    {
        if (earlier is Expr.RecordConstruction earlierRecord)
        {
            earlier = RecordExpressions.Create(earlierRecord);
        }

        if (later is Expr.RecordConstruction laterRecord)
        {
            later = RecordExpressions.Create(laterRecord);
        }

        // Restrict the result to total value construction so changing the arm
        // cannot affect evaluation or a source location in an exception stack.
        return (earlier, later) switch
        {
            (Expr.Variable left, Expr.Variable right) => left.Name == right.Name,
            (Expr.Literal left, Expr.Literal right) => left.Value.Equals(right.Value),
            (Expr.Tuple left, Expr.Tuple right) => left.Items.Count == right.Items.Count
                && left.Items.Zip(right.Items, SamePureCaseResult).All(equal => equal),
            _ => false
        };
    }
}
