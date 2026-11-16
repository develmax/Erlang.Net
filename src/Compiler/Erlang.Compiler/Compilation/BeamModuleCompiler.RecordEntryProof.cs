namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RecordProofScope(FunctionDefinition Function, Clause Clause, IReadOnlyList<Expr> Prefix);

        private bool HasProvenRecordInput(
            FunctionDefinition function,
            int input,
            string recordName,
            int recordArity
        )
        {
            var active = new HashSet<(string Function, int Arity, int? Parameter, int? Field)>();

            return ProveParameter(function, input);

            bool ProveParameter(FunctionDefinition target, int parameter)
            {
                if (owner.IsDeclaredNative((target.Name, target.Arity)))
                {
                    return false;
                }

                var key = (target.Name, target.Arity, (int?)parameter, (int?)null);
                if (!active.Add(key))
                {
                    // Every caller on the cycle must preserve the shape on successful return.
                    return true;
                }

                bool proven = HasOnlyLocalCalls(
                    target,
                    target.Arity,
                    (caller, invocation) =>
                {
                    Clause? clause = caller.Clauses.FirstOrDefault(candidate => HasUnshadowedInvocation(candidate, invocation));
                    if (clause is null)
                    {
                        return false;
                    }

                    IReadOnlyList<Expr> prefix = clause.Body is Expr.Sequence sequence
                        ? sequence.Items.TakeWhile(item => !LocalFunctionEntries.Walk(item).Any(value => ReferenceEquals(value, invocation))).ToArray()
                        : [];

                    return ProveValue(invocation.Arguments[parameter], null, new(caller, clause, prefix));
                }
                );
                active.Remove(key);

                return proven;
            }

            bool ProveResult(FunctionDefinition target, int? field)
            {
                if (owner.IsDeclaredNative((target.Name, target.Arity)))
                {
                    return false;
                }

                var key = (target.Name, target.Arity, (int?)null, field);
                if (!active.Add(key))
                {
                    return true;
                }

                bool proven = target.Clauses.All(clause => ProveValue(clause.Body, field, new(target, clause, [])));
                active.Remove(key);

                return proven;
            }

            bool ProveValue(Expr expression, int? field, RecordProofScope scope)
            {
                switch (expression)
                {
                    case Expr.RecordConstruction record when field is null:
                        return record.Definition.Name == recordName && record.Definition.Fields.Count + 1 == recordArity;
                    case Expr.RecordUpdate update when field is null:
                        return update.Name == recordName && update.Arity == recordArity;
                    case Expr.Tuple tuple:
                        return field is int index
                            ? index < tuple.Items.Count && ProveValue(tuple.Items[index], null, scope)
                            : tuple.Items.Count == recordArity
                                && tuple.Items[0] is Expr.Literal { Value: Atom tag } && tag.Name == recordName;
                    case Expr.Match match:
                        return ProveValue(match.Value, field, scope);
                    case Expr.Sequence sequence:
                        return ProveValue(sequence.Items[^1], field, scope with { Prefix = [.. scope.Prefix, .. sequence.Items.SkipLast(1)] });
                    case Expr.Block block:
                        return ProveValue(block.Body, field, scope);
                    case Expr.Case decision:
                        return decision.Clauses.All(branch => ProveValue(branch.Body, field, scope));
                    case Expr.If decision:
                        return decision.Clauses.All(branch => ProveValue(branch.Body, field, scope));
                    case Expr.Call { Module: null, DynamicRemote: false } call
                        when localFunctions.TryGetValue((call.Function, call.Arguments.Count), out var target):
                        return ProveResult(target, field);
                    case Expr.Call call when IsControlTransfer(call.Function, call.Arguments.Count) && owner.ResolvesErlangBif(call):
                        return true;
                    case Expr.Variable variable:
                        return ProveVariable(variable.Name, field, scope);
                    default:
                        return false;
                }
            }

            bool ProveVariable(string name, int? field, RecordProofScope scope)
            {
                for (int index = scope.Prefix.Count - 1; index >= 0; index--)
                {
                    if (scope.Prefix[index] is not Expr.Match match)
                    {
                        continue;
                    }

                    if (match.Pattern is Pattern.Variable variable && variable.Name == name)
                    {
                        return ProveValue(match.Value, field, scope with { Prefix = scope.Prefix.Take(index).ToArray() });
                    }

                    if (field is null && match.Pattern is Pattern.Tuple tuple)
                    {
                        int element = tuple.Items.ToList().FindIndex(pattern => pattern is Pattern.Variable item && item.Name == name);
                        if (element >= 0)
                        {
                            return ProveValue(match.Value, element, scope with { Prefix = scope.Prefix.Take(index).ToArray() });
                        }
                    }
                }

                for (int index = 0; index < scope.Clause.Patterns.Count; index++)
                {
                    Pattern pattern = scope.Clause.Patterns[index];
                    if (pattern is Pattern.Variable parameter && parameter.Name == name)
                    {
                        return field is null && ProveParameter(scope.Function, index);
                    }

                    if (field is null && pattern is AliasPattern alias
                        && TryReadPatternAlias(alias, out var binding, out var shape) && binding.Name == name
                        && shape is Pattern.Tuple tuple && tuple.Items.Count == recordArity
                        && tuple.Items[0] is Pattern.Literal { Value: Atom tag } && tag.Name == recordName)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
