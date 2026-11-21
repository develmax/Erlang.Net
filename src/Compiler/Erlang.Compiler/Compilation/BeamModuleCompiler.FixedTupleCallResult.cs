namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasFixedTupleCallResult(Expr.Call root, int arity)
        {
            var visited = new HashSet<(string Name, int Arity)>();
            var pending = new Stack<Expr>();
            bool hasTuple = false;
            pending.Push(root);
            while (pending.TryPop(out Expr? expression))
            {
                switch (expression)
                {
                    case Expr.Tuple tuple when tuple.Items.Count == arity:
                    case Expr.Literal { Value: TupleTerm tupleValue } when tupleValue.Items.Count == arity:
                        hasTuple = true;
                        break;
                    case Expr.Sequence { Items.Count: > 0 } sequence:
                        pending.Push(sequence.Items[^1]);
                        break;
                    case Expr.Block block:
                        pending.Push(block.Body);
                        break;
                    case Expr.Case decision:
                        PushClauses(decision.Clauses);
                        break;
                    case Expr.If conditional:
                        PushClauses(conditional.Clauses);
                        break;
                    case Expr.Call call when owner.IsKnownNonreturningLocalResult(call):
                        break;
                    case Expr.Call { Module: null, DynamicRemote: false } call:
                        var signature = (call.Function, call.Arguments.Count);
                        if (!localFunctions.TryGetValue(signature, out var callee) || owner.IsDeclaredNative(signature))
                        {
                            return false;
                        }

                        // A terminal local call contributes the callee's return
                        // leaves. Visiting each signature once closes recursive
                        // edges without treating a cycle alone as evidence.
                        if (visited.Add(signature))
                        {
                            PushClauses(callee.Clauses);
                        }

                        break;
                    default:
                        return false;
                }
            }

            return hasTuple;

            void PushClauses(IReadOnlyList<Clause> clauses)
            {
                foreach (Clause clause in clauses)
                {
                    pending.Push(clause.Body);
                }
            }
        }
    }
}
