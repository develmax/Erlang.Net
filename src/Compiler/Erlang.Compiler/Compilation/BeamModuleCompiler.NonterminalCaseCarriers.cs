namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static FunctionDefinition ReuseNonterminalCaseCarriers(FunctionDefinition function)
    {
        Clause[]? updated = null;
        int generatedAlias = 0;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Sequence sequence)
            {
                continue;
            }

            Expr[]? body = null;
            for (int item = 0; item + 1 < sequence.Items.Count; item++)
            {
                if (sequence.Items[item] is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard },
                        Value: Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false } } decision
                    } binding)
                {
                    continue;
                }

                Expr.Case replacement = AliasReconstructedCaseArms(decision, ref generatedAlias);
                if (!ReferenceEquals(decision, replacement))
                {
                    body ??= sequence.Items.ToArray();
                    body[item] = binding with { Value = replacement };
                }
            }

            if (body is not null)
            {
                updated ??= function.Clauses.ToArray();
                updated[index] = clause with { Body = sequence with { Items = body } };
            }
        }

        return updated is null ? function : function with { Clauses = updated };
    }

    private static Expr.Case AliasReconstructedCaseArms(Expr.Case decision, ref int generatedAlias)
    {
        Clause[]? updated = null;
        for (int index = 0; index < decision.Clauses.Count; index++)
        {
            Clause arm = decision.Clauses[index];
            if (arm is not { Guard: null, Patterns: [Pattern.Tuple shape] }
                || shape.Items.Count < 2
                || shape.Items[0] is not Pattern.Literal { Value: Atom }
                || !shape.Items.Skip(1).All(field => field is Pattern.Variable { Name: not VariableScopeNames.Wildcard }))
            {
                continue;
            }

            // The source fields keep their single-assignment identities in
            // descendant case clauses. Closures and comprehension scopes are
            // left to their own binding passes.
            string name = "$case_carrier_" + generatedAlias;
            Expr result = ReuseReconstructedCaseResult(arm.Body, shape, new Expr.Variable(name));
            if (!ReferenceEquals(result, arm.Body))
            {
                generatedAlias++;
                updated ??= decision.Clauses.ToArray();
                updated[index] = arm with
                {
                    Patterns = [new AliasPattern(new Pattern.Variable(name), shape)],
                    Body = result
                };
            }
        }

        return updated is null ? decision : decision with { Clauses = updated };
    }

    private static Expr ReuseReconstructedCaseResult(Expr expression, Pattern.Tuple shape, Expr.Variable alias)
    {
        if (expression is Expr.Tuple && ReconstructsMatchedValue(shape, expression))
        {
            return alias;
        }

        if (expression is Expr.Sequence { Items.Count: > 0 } sequence)
        {
            Expr terminal = ReuseReconstructedCaseResult(sequence.Items[^1], shape, alias);
            if (!ReferenceEquals(terminal, sequence.Items[^1]))
            {
                Expr[] items = sequence.Items.ToArray();
                items[^1] = terminal;

                return sequence with { Items = items };
            }
        }

        if (expression is Expr.Case decision)
        {
            Clause[]? updated = null;
            for (int index = 0; index < decision.Clauses.Count; index++)
            {
                Clause arm = decision.Clauses[index];
                Expr replacement = ReuseReconstructedCaseResult(arm.Body, shape, alias);
                if (!ReferenceEquals(replacement, arm.Body))
                {
                    updated ??= decision.Clauses.ToArray();
                    updated[index] = arm with { Body = replacement };
                }
            }

            if (updated is not null)
            {
                return decision with { Clauses = updated };
            }
        }

        return expression;
    }
}
