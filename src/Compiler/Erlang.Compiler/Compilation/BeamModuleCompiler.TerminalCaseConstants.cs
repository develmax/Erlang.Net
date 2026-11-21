namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static FunctionDefinition FoldTerminalCaseConstants(FunctionDefinition function)
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
            Expr rewritten = FoldCase(decision, bound);
            if (!ReferenceEquals(rewritten, decision))
            {
                updated ??= function.Clauses.ToArray();
                updated[index] = clause with { Body = rewritten };
            }
        }

        return updated is null ? function : function with { Clauses = updated };
    }

    private static Expr.Case FoldCase(Expr.Case decision, IReadOnlySet<string> bound)
    {
        if (decision.Value is not Expr.Variable selector || !bound.Contains(selector.Name))
        {
            return decision;
        }

        // A terminal arm cannot export its fresh variables to a continuation. An already
        // bound name is an equality check and must still participate in the emitted match.
        Clause[]? updated = null;
        for (int index = 0; index < decision.Clauses.Count; index++)
        {
            Clause arm = decision.Clauses[index];
            var armBound = arm.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
            armBound.UnionWith(bound);
            Expr output = arm.Body switch
            {
                Expr.Sequence { Items.Count: > 1 } sequence when ReadConstantBindings(sequence, armBound) is { } constants
                    && AttributeLiteral.TryNormalizeConstant(sequence.Items[^1], out Term literal, constants) => new Expr.Literal(literal),
                Expr.Case nested => FoldCase(nested, armBound),
                _ => arm.Body
            };
            if (!ReferenceEquals(output, arm.Body))
            {
                updated ??= decision.Clauses.ToArray();
                updated[index] = arm with { Body = output };
            }
        }

        return updated is null ? decision : decision with { Clauses = updated };
    }
}
