using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingletonCallTupleReturns(FunctionDefinition function)
        {
            if (recursiveClosure is not null || owner.OnLoadFunction is not null
                || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return false;
            }

            if (function.Clauses is [{ Guard: null, Patterns: [] } single])
            {
                if (ReadSingletonCallTuple(single.Body) is not { } plan)
                {
                    return false;
                }

                BeamSourceLocation previous = location;
                EmitSingletonCallTuple(plan, ref previous);

                return true;
            }

            if (function.Arity != 1 || function.Clauses.Count < 2)
            {
                return false;
            }

            var keys = new HashSet<Term>();
            var alternatives = new List<(Term Key, (Expr.Call[] Calls, Term Result) Plan)>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] }
                    || !keys.Add(key) || ReadSingletonCallTuple(clause.Body) is not { } plan)
                {
                    return false;
                }

                alternatives.Add((key, plan));
            }

            var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            int functionInfo = owner.nextLabel - 2;
            var branches = alternatives.OrderByDescending(branch => branch.Key, comparer)
                .Select(branch => (branch.Key, branch.Plan, Label: Label())).ToArray();
            owner.EmitMembershipSelect(
                0,
                functionInfo,
                branches.Reverse().Select(branch => (branch.Key, branch.Label))
            );
            BeamSourceLocation previousBranch = location;
            foreach (var branch in branches)
            {
                Mark(branch.Label);
                EmitSingletonCallTuple(branch.Plan, ref previousBranch);
            }

            return true;
        }

        private (Expr.Call[] Calls, Term Result)? ReadSingletonCallTuple(Expr body)
        {
            Expr.Tuple? tuple = body switch
            {
                Expr.Tuple literal => literal,
                Expr.RecordConstruction { Wildcard: null } record => RecordExpressions.Create(record),
                _ => null
            };
            if (tuple is null)
            {
                return null;
            }

            var calls = new List<Expr.Call>();
            var fields = new Term[tuple.Items.Count];
            for (int index = 0; index < fields.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } call)
                {
                    var signature = (call.Function, 0);
                    if (!entries.ContainsKey(signature) || owner.IsDeclaredNative(signature)
                        || !localFunctions.TryGetValue(signature, out FunctionDefinition? callee))
                    {
                        return null;
                    }

                    Term? result = ReadSingletonResult(callee);
                    if (result is not (Integer or Atom or Nil))
                    {
                        return null;
                    }

                    calls.Add(call);
                    fields[index] = result;
                }
                else if (!AttributeLiteral.TryNormalizeConstant(item, out fields[index]))
                {
                    return null;
                }
            }

            return calls.Count == 0 ? null : (calls.ToArray(), new TupleTerm(fields));
        }

        private void EmitSingletonCallTuple((Expr.Call[] Calls, Term Result) plan, ref BeamSourceLocation previous)
        {
            owner.EmitEmptyCallFrame(0);
            foreach (Expr.Call call in plan.Calls)
            {
                owner.EmitCallLine(call, previous);
                Call(null, call.Function, 0);
                previous = CallLocation(call, previous);
            }

            owner.EmitConstant(plan.Result, 0);
            owner.EmitFrameReturn(0);
        }
    }
}
