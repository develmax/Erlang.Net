namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static FunctionDefinition ReuseTerminalEffectfulCaseCarriers(FunctionDefinition function)
    {
        Clause[]? updated = null;
        int nextAlias = 0;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            Expr body = RewriteTerminalEffectfulCaseCarrier(clause.Body, ref nextAlias);
            if (!ReferenceEquals(body, clause.Body))
            {
                updated ??= function.Clauses.ToArray();
                updated[index] = clause with { Body = body };
            }
        }

        return updated is null ? function : function with { Clauses = updated };
    }

    private static Expr RewriteTerminalEffectfulCaseCarrier(Expr expression, ref int nextAlias)
    {
        if (expression is Expr.Sequence { Items.Count: > 0 } sequence)
        {
            Expr last = RewriteTerminalEffectfulCaseCarrier(sequence.Items[^1], ref nextAlias);
            if (!ReferenceEquals(last, sequence.Items[^1]))
            {
                Expr[] items = sequence.Items.ToArray();
                items[^1] = last;

                return sequence with { Items = items };
            }
        }

        if (expression is Expr.Block block)
        {
            Expr body = RewriteTerminalEffectfulCaseCarrier(block.Body, ref nextAlias);

            return ReferenceEquals(body, block.Body) ? block : block with { Body = body };
        }

        if (expression is not Expr.Case decision)
        {
            return expression;
        }

        bool reusesSelector = decision.Value is Expr.Call { DynamicRemote: false }
            && decision.Clauses.Count == 2
            && !decision.Clauses.Any(HasIdentityCaseFallback);
        Clause[]? updated = null;
        for (int index = 0; index < decision.Clauses.Count; index++)
        {
            Clause arm = decision.Clauses[index];
            Expr body = RewriteTerminalEffectfulCaseCarrier(arm.Body, ref nextAlias);
            Clause rewritten = ReferenceEquals(body, arm.Body) ? arm : arm with { Body = body };
            if (reusesSelector
                && rewritten is
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple shape],
                    Body: Expr.Sequence { Items: [Expr.Call { DynamicRemote: false } effect, Expr.Tuple rebuilt] } armBody
                }
                && shape.Items.Count >= 2 && shape.Items[0] is Pattern.Literal { Value: Atom }
                && shape.Items.Skip(1).All(field => field is Pattern.Variable { Name: not VariableScopeNames.Wildcard })
                && ReconstructsMatchedValue(shape, rebuilt))
            {
                var fields = shape.Items.Skip(1).Cast<Pattern.Variable>().Select(field => field.Name)
                    .ToHashSet(StringComparer.Ordinal);
                if (fields.Count == shape.Items.Count - 1 && HasBindingFreeSelector(effect)
                    && !LocalFunctionEntries.Walk(effect).OfType<Expr.Variable>().Any(variable => fields.Contains(variable.Name)))
                {
                    string alias = FormattableString.Invariant($"$terminal_case_carrier_{nextAlias++}");
                    // sys_core_alias binds the existing tuple to its constructor key.
                    // The call keeps its original arguments and source location.
                    rewritten = rewritten with
                    {
                        Patterns = [new AliasPattern(new Pattern.Variable(alias), shape)],
                        Body = armBody with { Items = [effect, new Expr.Variable(alias)] }
                    };
                }
            }

            if (decision.Value is Expr.Variable && AliasReconstructedListHead(rewritten, ref nextAlias) is { } carrier)
            {
                rewritten = carrier;
            }

            if (!ReferenceEquals(rewritten, arm))
            {
                updated ??= decision.Clauses.ToArray();
                updated[index] = rewritten;
            }
        }

        return updated is null ? decision : decision with { Clauses = updated };
    }

    private static Clause? AliasReconstructedListHead(Clause arm, ref int nextAlias)
    {
        if (arm is not
            {
                Patterns: [Pattern.Tuple shape],
                Body: Expr.List
                {
                    Items: [Expr.Tuple head],
                    Tail: Expr.Call { Module: null, DynamicRemote: false }
                } list
            }
            || shape.Items.Count is < 2 or >= CoreAliasDepthLimit
            || !shape.Items.All(item => item is Pattern.Variable { Name: not VariableScopeNames.Wildcard }
                or Pattern.Literal)
            || !ReconstructsMatchedValue(shape, head))
        {
            return null;
        }

        // Keep the checked value in the cons head while its tail call runs.
        // The pattern, guard and call still read the original field bindings.
        string alias = FormattableString.Invariant($"$case_list_head_carrier_{nextAlias++}");

        return arm with
        {
            Patterns = [new AliasPattern(new Pattern.Variable(alias), shape)],
            Body = list with { Items = [new Expr.Variable(alias)] }
        };
    }

    private static bool HasIdentityCaseFallback(Clause clause) => clause is
    {
        Guard: null,
        Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } input],
        Body: Expr.Variable result
    } && input.Name == result.Name;
}
