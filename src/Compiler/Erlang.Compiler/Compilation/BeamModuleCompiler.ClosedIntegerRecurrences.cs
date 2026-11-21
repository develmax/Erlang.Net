namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool IsClosedIntegerRecurrenceOperand(Expr.Binary expression)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || recursiveClosure is not null || owner.OnLoadFunction is not null
                || parentFunction is not { } function
                || expression is not
                {
                    Operator: ErlangOperators.Minus,
                    Left: Expr.Variable input,
                    Right: Expr.Literal { Value: Integer step }
                } || step.Value.Sign <= 0)
            {
                return false;
            }

            foreach (Clause clause in function.Clauses)
            {
                foreach (Expr.Call call in LocalFunctionEntries.Walk(clause.Body).OfType<Expr.Call>())
                {
                    if (call.Module is not null || call.DynamicRemote
                        || call.Function != function.Name || call.Arguments.Count != function.Arity)
                    {
                        continue;
                    }

                    for (int index = 0; index < call.Arguments.Count; index++)
                    {
                        if (ReferenceEquals(call.Arguments[index], expression)
                            && clause.Patterns[index] is Pattern.Variable counter && counter.Name == input.Name)
                        {
                            // The incoming edges establish an integer domain.
                            // The update does not establish a bounded range.
                            return owner.ReadClosedIntegerRecurrence(function, index, localFunctions);
                        }
                    }
                }
            }

            return false;
        }
    }

    private bool ReadClosedIntegerRecurrence(
        FunctionDefinition target,
        int argument,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> definitions
    )
    {
        var signature = (target.Name, target.Arity);
        if (exportedFunctions.Contains(signature) || IsDeclaredNative(signature)
            || signature == (OnLoadFunction, 0))
        {
            return false;
        }

        bool initialized = false;
        foreach (FunctionDefinition caller in definitions.Values)
        {
            foreach (Clause clause in caller.Clauses)
            {
                Expr[] expressions = LocalFunctionEntries.ClauseExpressions([clause])
                    .SelectMany(LocalFunctionEntries.Walk).ToArray();
                bool nested = expressions.Any(expression => expression is
                    Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension);
                foreach (Expr expression in expressions)
                {
                    if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                    {
                        return false;
                    }

                    if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                        || (call.Function, call.Arguments.Count) != signature)
                    {
                        continue;
                    }

                    Expr input = call.Arguments[argument];
                    if (input is Expr.Literal { Value: Integer })
                    {
                        initialized = true;
                        continue;
                    }

                    if (nested || (caller.Name, caller.Arity) != signature
                        || clause.Patterns[argument] is not Pattern.Variable counter
                        || counter.Name == VariableScopeNames.Wildcard)
                    {
                        return false;
                    }

                    if (input is Expr.Variable forwarded && forwarded.Name == counter.Name)
                    {
                        continue;
                    }

                    if (input is not Expr.Binary
                        {
                            Operator: ErlangOperators.Plus or ErlangOperators.Minus,
                            Left: Expr.Variable previous, Right: var step
                        } || previous.Name != counter.Name)
                    {
                        return false;
                    }

                    if (step is Expr.Literal { Value: Integer })
                    {
                        continue;
                    }

                    if (step is not Expr.Variable increment || clause.Body is not Expr.Sequence
                        {
                            Items: [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable binding,
                                Value: Expr.Call { Module: null, DynamicRemote: false } producer
                            },
                                var next
                            ]
                        } || !ReferenceEquals(call, next) || binding.Name != increment.Name
                        || !definitions.TryGetValue((producer.Function, producer.Arguments.Count), out var callee)
                        || ReadLiteralIntegerResult(callee) is null)
                    {
                        return false;
                    }
                }
            }
        }

        return initialized;
    }

    private (long Lower, long Upper)? ReadLocalRange(
        int entry,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> definitions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        foreach (var item in localEntries)
        {
            int label = item.Value;
            if (label <= 0 && !deferredEntries.TryGetValue(label, out label))
            {
                continue;
            }

            if (MapEmittedLabel(label) == entry && definitions.TryGetValue(item.Key, out var function))
            {
                return ReadLiteralIntegerResult(function);
            }
        }

        return null;
    }

    private (long Lower, long Upper)? ReadLiteralIntegerResult(FunctionDefinition function)
    {
        if (IsDeclaredNative((function.Name, function.Arity)) || function.IsInlineFailure)
        {
            return null;
        }

        long min = long.MaxValue;
        long max = long.MinValue;
        if (function.Clauses.Count == 0 || !function.Clauses.All(clause => ReadResult(clause.Body)))
        {
            return null;
        }

        return (min, max);

        bool ReadResult(Expr expression)
        {
            switch (expression)
            {
                case Expr.Literal { Value: Integer value } when value.Value >= long.MinValue && value.Value <= long.MaxValue:
                    min = Math.Min(min, (long)value.Value);
                    max = Math.Max(max, (long)value.Value);

                    return true;
                case Expr.Case { Clauses.Count: > 0 } decision:
                    return decision.Clauses.All(clause => ReadResult(clause.Body));
                case Expr.If { Clauses.Count: > 0 } decision:
                    return decision.Clauses.All(clause => ReadResult(clause.Body));
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return ReadResult(sequence.Items[^1]);
                case Expr.Block block:
                    return ReadResult(block.Body);
                default:
                    return false;
            }
        }
    }
}
