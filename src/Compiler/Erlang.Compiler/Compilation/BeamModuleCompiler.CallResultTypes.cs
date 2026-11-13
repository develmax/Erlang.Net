namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ResultProjection(Pattern.Tuple Pattern, int Field);

        private readonly Dictionary<(string Name, int Arity), int> callResultTypes = [];

        private bool ReturnsLocalCallConstant(EntryCall call, Term expected)
        {
            var signature = (call.Call.Function, call.Call.Arguments.Count);
            return !owner.ModuleOptimizationDisabled && call.Module is null
                && !owner.IsDeclaredNative(signature)
                && localFunctions.TryGetValue(signature, out var definition)
                && definition.Clauses.All(clause => ReturnsConstant(clause.Body));

            bool ReturnsConstant(Expr expression) => expression switch
            {
                Expr.Sequence sequence => ReturnsConstant(sequence.Items[^1]),
                Expr.Block block => ReturnsConstant(block.Body),
                Expr.Case decision => decision.Clauses.All(clause => ReturnsConstant(clause.Body)),
                Expr.If conditional => conditional.Clauses.All(clause => ReturnsConstant(clause.Body)),
                _ => AttributeLiteral.TryNormalizeConstant(expression, out Term value) && expected.Equals(value)
            };
        }

        private static bool HasKnownNonTupleResult(EntryCall call, Atom alternative) =>
            (call.Module, call.Call.Function, call.Call.Arguments.Count, alternative.Name) is
                ("lists", "keyfind", 3, "false") or ("maps", "find" or "take", 2, "error");

        private static bool ReturnsLookupResult(string? module, string function, int arity) =>
            (module, function, arity) is ("lists", "keyfind", 3) or ("maps", "find" or "take", 2);

        private Term? ReadConstantCallProjection(EntryCall selector, Pattern.Tuple pattern, int field)
        {
            if (selector.Module is not null)
            {
                return null;
            }

            Term? constant = null;
            var active = new HashSet<(string Name, int Arity)>();

            return Read(selector.Call) ? constant : null;

            bool Read(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Call { Module: null, DynamicRemote: false } call:
                        var signature = (call.Function, call.Arguments.Count);
                        if (!localFunctions.TryGetValue(signature, out var definition)
                            || owner.IsDeclaredNative(signature) || !active.Add(signature))
                        {
                            return false;
                        }

                        bool known = definition.Clauses.All(clause => Read(clause.Body));
                        active.Remove(signature);
                        return known;
                    case Expr.Sequence { Items.Count: > 0 } sequence:
                        return Read(sequence.Items[^1]);
                    case Expr.Block block:
                        return Read(block.Body);
                    case Expr.Case decision:
                        return decision.Clauses.All(clause => Read(clause.Body));
                    case Expr.If conditional:
                        return conditional.Clauses.All(clause => Read(clause.Body));
                    case Expr.RecordConstruction record:
                        return Read(RecordExpressions.Create(record));
                    case Expr.Tuple tuple:
                        return ReadTuple(tuple.Items);
                    default:
                        if (!AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                        {
                            return false;
                        }

                        return value is not TupleTerm literal || ReadTuple(literal.Items.Select(item => (Expr)new Expr.Literal(item)).ToArray());
                }
            }

            bool ReadTuple(IReadOnlyList<Expr> items)
            {
                if (items.Count != pattern.Items.Count)
                {
                    return true;
                }

                for (int index = 0; index < items.Count; index++)
                {
                    if (pattern.Items[index] is Pattern.Literal expected
                        && AttributeLiteral.TryNormalizeConstant(items[index], out Term actual)
                        && !expected.Value.Equals(actual))
                    {
                        return true;
                    }
                }

                if (!AttributeLiteral.TryNormalizeConstant(items[field], out Term projected)
                    || constant is not null && !constant.Equals(projected))
                {
                    return false;
                }

                constant = projected;
                return true;
            }
        }

        private int ReadCallResultUnion(Expr.Call call)
        {
            var signature = (call.Function, call.Arguments.Count);
            if (call.DynamicRemote || call.Module is not null || !localFunctions.ContainsKey(signature))
            {
                return 0;
            }

            if (!callResultTypes.TryGetValue(signature, out int type))
            {
                type = ReadResultType(call, []);
                callResultTypes.Add(signature, type);
            }

            return type switch
            {
                (AtomTypeBits | TupleTypeBits) or (AtomTypeBits | ConsTypeBits)
                    or (AtomTypeBits | ProperListTypeBits) or (AtomTypeBits | (ProperListTypeBits & ~ConsTypeBits)) => type,
                _ => 0
            };
        }

        private int ReadResultType(Expr expression, HashSet<(string Name, int Arity, ResultProjection? Projection)> visited,
            IReadOnlyDictionary<string, int>? bindings = null, bool selectorContext = false, ResultProjection? projection = null)
        {
            if (projection is not null && expression is Expr.Tuple tuple)
            {
                if (tuple.Items.Count != projection.Pattern.Items.Count)
                {
                    return 0;
                }
                for (int index = 0; index < tuple.Items.Count; index++)
                {
                    if (projection.Pattern.Items[index] is Pattern.Literal expected
                        && AttributeLiteral.TryNormalizeConstant(tuple.Items[index], out Term actual)
                        && !expected.Value.Equals(actual))
                    {
                        return 0;
                    }
                }
                return ReadResultType(tuple.Items[projection.Field], visited, bindings, selectorContext: true);
            }

            switch (expression)
            {
                case Expr.Literal { Value: Atom }:
                    return projection is null ? AtomTypeBits : 0;
                case Expr.Literal { Value: TupleTerm } or Expr.Tuple or Expr.RecordConstruction:
                    return projection is null ? TupleTypeBits : AnyTypeBits;
                case Expr.Literal { Value: Cons } or Expr.List { Items.Count: > 0 }:
                    return projection is null ? ConsTypeBits : 0;
                case Expr.Literal { Value: Nil }:
                    return projection is null ? ProperListTypeBits & ~ConsTypeBits : 0;
                case Expr.List { Items.Count: 0, Tail: null }:
                    return projection is null ? ProperListTypeBits & ~ConsTypeBits : 0;
                case Expr.Variable variable:
                    return projection is null ? bindings?.GetValueOrDefault(variable.Name, AnyTypeBits) ?? AnyTypeBits : AnyTypeBits;
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return ReadResultType(sequence.Items[^1], visited, bindings, selectorContext, projection);
                case Expr.Block block:
                    return ReadResultType(block.Body, visited, bindings, selectorContext, projection);
                case Expr.Case decision:
                    return ReadCaseResultTypes(decision, visited, bindings, selectorContext, projection);
                case Expr.If decision:
                    return ReadBranchResultTypes(decision.Clauses, visited, bindings, selectorContext, projection);
                case Expr.Call { Module: null, DynamicRemote: false } call:
                    var signature = (call.Function, call.Arguments.Count);
                    if (!localFunctions.TryGetValue(signature, out var local) || owner.IsDeclaredNative(signature))
                    {
                        return ReadExternalResultType(call, owner.ResolveExternalModule(call.Function, signature.Count), projection);
                    }

                    var visit = (signature.Function, signature.Count, projection);
                    if (!visited.Add(visit))
                    {
                        // A recursive selector may bind a value from any reachable return path.
                        return selectorContext && projection is null ? AnyTypeBits : 0;
                    }

                    int result = ReadBranchResultTypes(local.Clauses, visited, selectorContext: selectorContext, projection: projection);
                    visited.Remove(visit);
                    return result;
                case Expr.Call { DynamicRemote: false } call:
                    return ReadExternalResultType(call, call.Module, projection);
                default:
                    return AnyTypeBits;
            }
        }

        private static int ReadExternalResultType(Expr.Call call, string? module, ResultProjection? projection)
        {
            if ((module, call.Function, call.Arguments.Count) is ("lists", "reverse", 1))
            {
                return projection is null ? ReadReverseResult(call) : 0;
            }

            return projection is null && ReturnsLookupResult(module, call.Function, call.Arguments.Count)
                ? AtomTypeBits | TupleTypeBits
                : AnyTypeBits;
        }

        private static int ReadReverseResult(Expr.Call reverse) =>
            reverse.Arguments[0] switch
            {
                Expr.List { Items.Count: > 0 } or Expr.Literal { Value: Cons } => ConsTypeBits,
                Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil } => ProperListTypeBits & ~ConsTypeBits,
                _ => ProperListTypeBits
            };

        private int ReadCaseResultTypes(Expr.Case decision, HashSet<(string Name, int Arity, ResultProjection? Projection)> visited,
            IReadOnlyDictionary<string, int>? bindings, bool selectorContext, ResultProjection? projection)
        {
            int? selector = null;
            int result = 0;
            foreach (Clause clause in decision.Clauses)
            {
                var branch = bindings;
                if (clause.Patterns is [Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable])
                {
                    var matched = bindings is null ? new Dictionary<string, int>(StringComparer.Ordinal)
                        : new Dictionary<string, int>(bindings, StringComparer.Ordinal);
                    matched[variable.Name] = selector ??= ReadResultType(decision.Value, visited, bindings, selectorContext: true);
                    branch = matched;
                }
                else if (projection is null && clause.Patterns is [Pattern.Tuple tuple]
                    && ReadReturnedVariable(clause.Body) is { } returned)
                {
                    int field = Array.FindIndex(tuple.Items.ToArray(), item => item is Pattern.Variable binding
                        && binding.Name == returned.Name && binding.Name != VariableScopeNames.Wildcard);
                    if (field >= 0)
                    {
                        var matched = bindings is null ? new Dictionary<string, int>(StringComparer.Ordinal)
                            : new Dictionary<string, int>(bindings, StringComparer.Ordinal);
                        matched[returned.Name] = ReadResultType(decision.Value, visited, bindings,
                            selectorContext: true, projection: new ResultProjection(tuple, field));
                        branch = matched;
                    }
                }

                result |= ReadResultType(clause.Body, visited, branch, selectorContext, projection);
            }

            return result;
        }

        private static Expr.Variable? ReadReturnedVariable(Expr expression) => expression switch
        {
            Expr.Variable variable => variable,
            Expr.Sequence { Items.Count: > 0 } sequence => ReadReturnedVariable(sequence.Items[^1]),
            Expr.Block block => ReadReturnedVariable(block.Body),
            _ => null
        };

        private int ReadBranchResultTypes(
            IReadOnlyList<Clause> clauses,
            HashSet<(string Name, int Arity, ResultProjection? Projection)> visited,
            IReadOnlyDictionary<string, int>? bindings = null,
            bool selectorContext = false,
            ResultProjection? projection = null
        )
        {
            int result = 0;
            foreach (Clause clause in clauses)
            {
                result |= ReadResultType(clause.Body, visited, bindings, selectorContext, projection);
                if (result == AnyTypeBits)
                {
                    break;
                }
            }

            return result;
        }
    }
}
