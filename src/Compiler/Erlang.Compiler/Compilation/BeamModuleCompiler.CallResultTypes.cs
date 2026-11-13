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
                    or (AtomTypeBits | ProperListTypeBits) => type,
                _ => 0
            };
        }

        private int ReadResultType(Expr expression, HashSet<(string Name, int Arity)> visited)
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
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return ReadResultType(sequence.Items[^1], visited);
                case Expr.Block block:
                    return ReadResultType(block.Body, visited);
                case Expr.Case decision:
                    return ReadBranchResultTypes(decision.Clauses, visited);
                case Expr.If decision:
                    return ReadBranchResultTypes(decision.Clauses, visited);
                case Expr.Call { Module: null, DynamicRemote: false } call:
                    var signature = (call.Function, call.Arguments.Count);
                    if (!localFunctions.TryGetValue(signature, out var local) || owner.IsDeclaredNative(signature))
                    {
                        return AnyTypeBits;
                    }

                    if (!visited.Add(signature))
                    {
                        // The first visit accounts for every return path in this function.
                        return 0;
                    }

                    return ReadBranchResultTypes(local.Clauses, visited);
                default:
                    return AnyTypeBits;
            }
        }

        private int ReadBranchResultTypes(
            IReadOnlyList<Clause> clauses,
            HashSet<(string Name, int Arity)> visited
        )
        {
            int result = 0;
            foreach (Clause clause in clauses)
            {
                result |= ReadResultType(clause.Body, visited);
                if (result == AnyTypeBits)
                {
                    break;
                }
            }

            return result;
        }
    }
}
