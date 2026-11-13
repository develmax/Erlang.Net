namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly Dictionary<(string Name, int Arity), int> callResultTypes = [];

        private static bool HasKnownNonTupleResult(EntryCall call, Atom alternative) =>
            (call.Module, call.Call.Function, call.Call.Arguments.Count, alternative.Name) is
                ("lists", "keyfind", 3, "false") or ("maps", "find" or "take", 2, "error");

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

        private int ReadResultType(Expr expression, HashSet<(string Name, int Arity)> visited,
            IReadOnlyDictionary<string, int>? bindings = null, bool selectorContext = false)
        {
            switch (expression)
            {
                case Expr.Literal { Value: Atom }:
                    return AtomTypeBits;
                case Expr.Literal { Value: TupleTerm } or Expr.Tuple or Expr.RecordConstruction:
                    return TupleTypeBits;
                case Expr.Literal { Value: Cons } or Expr.List { Items.Count: > 0 }:
                    return ConsTypeBits;
                case Expr.Literal { Value: Nil }:
                    return ProperListTypeBits & ~ConsTypeBits;
                case Expr.List { Items.Count: 0, Tail: null }:
                    return ProperListTypeBits & ~ConsTypeBits;
                case Expr.Variable variable:
                    return bindings?.GetValueOrDefault(variable.Name, AnyTypeBits) ?? AnyTypeBits;
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return ReadResultType(sequence.Items[^1], visited, bindings, selectorContext);
                case Expr.Block block:
                    return ReadResultType(block.Body, visited, bindings, selectorContext);
                case Expr.Case decision:
                    return ReadCaseResultTypes(decision, visited, bindings, selectorContext);
                case Expr.If decision:
                    return ReadBranchResultTypes(decision.Clauses, visited, bindings, selectorContext);
                case Expr.Call { Module: null, DynamicRemote: false } call:
                    var signature = (call.Function, call.Arguments.Count);
                    if (!localFunctions.TryGetValue(signature, out var local) || owner.IsDeclaredNative(signature))
                    {
                        return AnyTypeBits;
                    }

                    if (!visited.Add(signature))
                    {
                        // A recursive selector may bind a value from any reachable return path.
                        return selectorContext ? AnyTypeBits : 0;
                    }

                    int result = ReadBranchResultTypes(local.Clauses, visited, selectorContext: selectorContext);
                    visited.Remove(signature);
                    return result;
                case Expr.Call { Module: "lists", Function: "reverse", DynamicRemote: false, Arguments.Count: 1 }:
                    return ProperListTypeBits;
                default:
                    return AnyTypeBits;
            }
        }

        private int ReadCaseResultTypes(Expr.Case decision, HashSet<(string Name, int Arity)> visited,
            IReadOnlyDictionary<string, int>? bindings, bool selectorContext)
        {
            int selector = ReadResultType(decision.Value, visited, bindings, selectorContext: true);
            int result = 0;
            foreach (Clause clause in decision.Clauses)
            {
                var branch = bindings;
                if (clause.Patterns is [Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable])
                {
                    var matched = bindings is null ? new Dictionary<string, int>(StringComparer.Ordinal)
                        : new Dictionary<string, int>(bindings, StringComparer.Ordinal);
                    matched[variable.Name] = selector;
                    branch = matched;
                }
                result |= ReadResultType(clause.Body, visited, branch, selectorContext);
            }

            return result;
        }

        private int ReadBranchResultTypes(
            IReadOnlyList<Clause> clauses,
            HashSet<(string Name, int Arity)> visited,
            IReadOnlyDictionary<string, int>? bindings = null,
            bool selectorContext = false
        )
        {
            int result = 0;
            foreach (Clause clause in clauses)
            {
                result |= ReadResultType(clause.Body, visited, bindings, selectorContext);
                if (result == AnyTypeBits)
                {
                    break;
                }
            }

            return result;
        }
    }
}
