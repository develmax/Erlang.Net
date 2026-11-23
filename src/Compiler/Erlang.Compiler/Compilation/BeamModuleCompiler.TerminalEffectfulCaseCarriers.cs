namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition ReuseTerminalEffectfulCaseCarriers(FunctionDefinition function)
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

    private Expr RewriteTerminalEffectfulCaseCarrier(Expr expression, ref int nextAlias)
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

        if (SsaTypeInferenceEnabled && AliasCheckedCaseAfterAttempt(decision, ref nextAlias) is { } retained)
        {
            return retained;
        }

        if (SsaTypeInferenceEnabled && decision.Value is Expr.Call { DynamicRemote: false }
            && decision.Clauses is
            [
            {
                Guard: null,
                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Variable] }],
                Body: Expr.Case
                {
                    Value: Expr.Call { DynamicRemote: false },
                    Clauses:
                        [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom }], Body: Expr.Tuple },
                            var innerFallback
                        ]
                }
            },
                var outerFallback
            ]
            && HasIdentityCaseFallback(innerFallback) && HasIdentityCaseFallback(outerFallback))
        {
            Expr.Case aliased = AliasReconstructedCaseArms(decision, ref nextAlias);
            if (!ReferenceEquals(aliased, decision))
            {
                return aliased;
            }
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

    private static Expr.Case? AliasCheckedCaseAfterAttempt(Expr.Case decision, ref int nextAlias)
    {
        if (decision.Value is not Expr.Call { DynamicRemote: false } producer
            || !HasPreparedProducerArguments(producer)
            || decision.Clauses is not [
            {
                Guard: null,
                Patterns: [
                        Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Variable payload] } shape
                    ],
                Body: Expr.Case
                {
                    Value: Expr.Call { DynamicRemote: false } attempt,
                    Clauses: [
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom }],
                        Body: Expr.Tuple rebuilt
                    } accepted,
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable failure],
                        Body: Expr.Sequence
                        {
                            Items: [Expr.Call { DynamicRemote: false } cleanup, Expr.Variable returned]
                        }
                    } rejected
                        ]
                } continuation
            } success,
                var fallback
            ] || !HasIdentityCaseFallback(fallback) || payload.Name == VariableScopeNames.Wildcard
            || failure.Name == VariableScopeNames.Wildcard || failure.Name == payload.Name || failure.Name != returned.Name
            || !ReconstructsMatchedValue(shape, rebuilt) || !attempt.Arguments.All(IsPreparedOperand)
            || attempt.Arguments.OfType<Expr.Variable>().Count(argument => argument.Name == payload.Name) != 1
            || cleanup.Arguments is not [Expr.Variable resource] || resource.Name != payload.Name)
        {
            return null;
        }

        string alias = FormattableString.Invariant($"$checked_attempt_carrier_{nextAlias++}");

        // The producer, attempt and cleanup are unchanged. The original checked
        // tuple is read only on the accepted edge of the second call.
        return decision with
        {
            Clauses =
            [
                success with
                {
                    Patterns = [new AliasPattern(new Pattern.Variable(alias), shape)],
                    Body = continuation with
                    {
                        Clauses = [accepted with { Body = new Expr.Variable(alias) }, rejected]
                    }
                },
                fallback
            ]
        };

        static bool HasPreparedProducerArguments(Expr.Call producer)
        {
            bool hasComputedArgument = false;
            foreach (Expr argument in producer.Arguments)
            {
                if (IsPreparedOperand(argument))
                {
                    continue;
                }

                if (hasComputedArgument || argument is not Expr.Call { Module: null, DynamicRemote: false } computed
                    || !computed.Arguments.All(IsPreparedOperand))
                {
                    return false;
                }

                // This operand is still evaluated by the original producer.
                // It introduces no binding in the checked pattern or its arms.
                hasComputedArgument = true;
            }

            return true;
        }

        static bool IsPreparedOperand(Expr expression) =>
            expression is Expr.Variable or Expr.RecordAccess { Value: Expr.Variable }
            || AttributeLiteral.TryNormalizeConstant(expression, out _);
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
