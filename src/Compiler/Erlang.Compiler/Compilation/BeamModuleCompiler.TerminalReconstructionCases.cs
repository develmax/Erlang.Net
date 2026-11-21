namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static FunctionDefinition FoldTerminalReconstructionCases(FunctionDefinition function)
    {
        Clause[]? updated = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Case decision)
            {
                continue;
            }

            var bound = clause.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
            var result = FoldTerminalReconstructionCase(decision, bound);
            if (!ReferenceEquals(result, decision))
            {
                updated ??= function.Clauses.ToArray();
                updated[index] = clause with { Body = result };
            }
        }

        return updated is null ? function : function with { Clauses = updated };
    }

    private static Expr FoldTerminalReconstructionCase(Expr.Case decision, HashSet<string> bound)
    {
        if (!HasBindingFreeSelector(decision.Value))
        {
            return decision;
        }

        if (decision.Clauses.Count > 0
            && decision.Clauses[^1] is { Guard: null, Patterns: [Pattern.Variable fallback] }
            && fallback.Name != VariableScopeNames.Wildcard && !bound.Contains(fallback.Name)
            && decision.Clauses.All(arm => arm is { Guard: null, Patterns: [var pattern] }
                && ReconstructsMatchedValue(pattern, arm.Body)))
        {
            // The selector is evaluated exactly once. Every successful arm
            // returns its unchanged value and the fresh final binding is exhaustive.
            return decision.Value;
        }

        Clause[]? updated = null;
        for (int index = 0; index < decision.Clauses.Count; index++)
        {
            Clause arm = decision.Clauses[index];
            if (arm.Body is not Expr.Case nested)
            {
                continue;
            }

            var armScope = new HashSet<string>(bound, StringComparer.Ordinal);
            armScope.UnionWith(arm.Patterns.SelectMany(Semantics.Variables));
            Expr body = FoldTerminalReconstructionCase(nested, armScope);
            if (!ReferenceEquals(body, nested))
            {
                updated ??= decision.Clauses.ToArray();
                updated[index] = arm with { Body = body };
            }
        }

        return updated is null ? decision : decision with { Clauses = updated };
    }

    private static bool HasBindingFreeSelector(Expr selector) => selector switch
    {
        Expr.Variable or Expr.Literal => true,
        Expr.Call { DynamicRemote: false } call => call.Arguments.All(HasBindingFreeSelector),
        Expr.Tuple tuple => tuple.Items.All(HasBindingFreeSelector),
        Expr.List list => list.Items.All(HasBindingFreeSelector)
            && (list.Tail is null || HasBindingFreeSelector(list.Tail)),
        _ => AttributeLiteral.TryNormalizeConstant(selector, out _)
    };

    private static bool ReconstructsMatchedValue(Pattern pattern, Expr expression) => (pattern, expression) switch
    {
        (Pattern.Literal literal, Expr.Literal value) => literal.Value.Equals(value.Value),
        (Pattern.Variable binding, Expr.Variable value) => binding.Name != VariableScopeNames.Wildcard && binding.Name == value.Name,
        (AliasPattern alias, _) => ReconstructsMatchedValue(alias.Left, expression)
            || ReconstructsMatchedValue(alias.Right, expression),
        (Pattern.Tuple target, Expr.Tuple result) => target.Items.Count == result.Items.Count
            && target.Items.Zip(result.Items, ReconstructsMatchedValue).All(match => match),
        (Pattern.List target, Expr.List result) => target.Items.Count == result.Items.Count
            && target.Items.Zip(result.Items, ReconstructsMatchedValue).All(match => match)
            && (target.Tail is null && result.Tail is null
                || target.Tail is { } tail && result.Tail is { } value && ReconstructsMatchedValue(tail, value)),
        _ => false
    };
}
