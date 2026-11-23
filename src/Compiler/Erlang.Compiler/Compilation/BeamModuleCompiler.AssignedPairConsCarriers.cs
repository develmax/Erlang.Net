namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition ReuseAssignedPairConsHeads(FunctionDefinition function)
    {
        if (!SsaTypeInferenceEnabled)
        {
            return function;
        }

        Clause[]? updated = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause clause = function.Clauses[index];
            if (clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable left, Pattern.Variable right] } pair,
                        Value: Expr.Call { Module: not null, DynamicRemote: false } producer
                    } binding,
                        Expr.Case { Value: Expr.Variable selector } decision
                    ]
                } sequence
                || !DistinctBindings(left, right) || selector.Name != left.Name && selector.Name != right.Name
                || !HasBindingFreeSelector(producer)
                || clause.Patterns.SelectMany(Semantics.Variables).Any(name => name == left.Name || name == right.Name))
            {
                continue;
            }

            var alias = new Pattern.Variable(FormattableString.Invariant($"$assigned_pair_cons_{index}"));
            Expr replacement = Rewrite(decision, pair, new Expr.Variable(alias.Name));
            if (ReferenceEquals(replacement, decision))
            {
                continue;
            }

            // sys_core_alias retains the checked constructor's source value.
            // Retain both bindings for dispatch, calls and unmatched patterns.
            updated ??= function.Clauses.ToArray();
            updated[index] = clause with
            {
                Body = sequence with { Items = [binding with { Pattern = new AliasPattern(alias, pair) }, replacement] }
            };
        }

        return updated is null ? function : function with { Clauses = updated };

        Expr Rewrite(Expr expression, Pattern.Tuple pair, Expr.Variable alias)
        {
            switch (expression)
            {
                case Expr.Case decision:
                    {
                        Clause[]? clauses = null;
                        for (int index = 0; index < decision.Clauses.Count; index++)
                        {
                            Clause branch = decision.Clauses[index];
                            Expr replacement = Rewrite(branch.Body, pair, alias);
                            if (!ReferenceEquals(replacement, branch.Body))
                            {
                                clauses ??= decision.Clauses.ToArray();
                                clauses[index] = branch with { Body = replacement };
                            }
                        }

                        return clauses is null ? decision : decision with { Clauses = clauses };
                    }
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    {
                        Expr terminal = Rewrite(sequence.Items[^1], pair, alias);
                        if (ReferenceEquals(terminal, sequence.Items[^1]))
                        {
                            return sequence;
                        }

                        Expr[] values = sequence.Items.ToArray();
                        values[^1] = terminal;

                        return sequence with { Items = values };
                    }
                case Expr.Call { Module: null, DynamicRemote: false } recur
                    when recur.Function == function.Name && recur.Arguments.Count == function.Arity:
                    {
                        Expr[]? arguments = null;
                        for (int index = 0; index < recur.Arguments.Count; index++)
                        {
                            if (recur.Arguments[index] is Expr.List { Items: [Expr.Tuple rebuilt], Tail: Expr.Variable } cons
                                && ReconstructsMatchedValue(pair, rebuilt))
                            {
                                arguments ??= recur.Arguments.ToArray();
                                arguments[index] = cons with { Items = [alias] };
                            }
                        }

                        return arguments is null ? recur : recur with { Arguments = arguments };
                    }
                default:
                    return expression;
            }
        }
    }
}
