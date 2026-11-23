using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalSingletonListCase(Expr expression, bool inputPrepared = false)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.receiveScratchRegisters is not null
                || deferredPairMembers.Count != 0 || expression is not Expr.Case { Value: Expr.Call selector } decision)
            {
                return false;
            }

            var clauses = decision.Clauses;
            if (clauses is
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }], Body: var missing },
                { Guard: null, Patterns: [Pattern.Variable returned], Body: Expr.Variable result }
                ] && returned.Name != VariableScopeNames.Wildcard && returned.Name == result.Name
                && !variables.ContainsKey(returned.Name) && !deferredPairMembers.ContainsKey(returned.Name)
                && HasLocalTail(missing))
            {
                PrepareSelector();
                int present = Label();
                owner.Op(IsNil, LabelTag, present);
                owner.Operand(XRegisterTag, 0);
                TailLocal(missing, reuseCallSlots: true);
                Mark(present);
                owner.EmitFrameReturn(frameSize);

                return true;
            }

            if (clauses.Count < 3 || clauses[^1] is not
                { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard } or Erlang.Pattern.Any], Body: var fallback }
                || !HasLocalTail(fallback))
            {
                return false;
            }

            var arms = new List<(Integer Key, Atom Result)>();
            var keys = new HashSet<Integer>();
            foreach (Clause clause in clauses.SkipLast(1))
            {
                if (clause.Guard is not null || clause.Patterns is not [var pattern]
                    || ReadSingletonInteger(pattern) is not { } key || !keys.Add(key)
                    || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term resultTerm) || resultTerm is not Atom constant)
                {
                    return false;
                }

                arms.Add((key, constant));
            }

            // The integer switch visits its largest head key first and shares labels for identical outcomes.
            var groups = arms.GroupBy(arm => arm.Result).OrderByDescending(group => group.Max(arm => arm.Key.Value)).ToArray();
            if (groups.Length < 2)
            {
                return false;
            }

            PrepareSelector();
            int[] targets = groups.Select(_ => Label()).ToArray();
            int reject = Label();
            owner.Op(IsNonemptyList, LabelTag, reject);
            owner.Operand(XRegisterTag, 0);
            // The wildcard fallback cannot observe the selector after extracting its head and tail.
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitMembershipSelect(
                1,
                reject,
                groups.SelectMany((group, index) => group.Select(arm => (Key: (Term)arm.Key, Label: targets[index])))
                    .OrderBy(arm => ((Integer)arm.Key).Value)
            );
            for (int index = 0; index < targets.Length; index++)
            {
                Mark(targets[index]);
                owner.Op(IsNil, LabelTag, reject);
                owner.Operand(XRegisterTag, 0);
                owner.EmitConstant(groups[index].Key, 0);
                owner.EmitFrameReturn(frameSize);
            }
            Mark(reject);
            TailLocal(fallback, reuseCallSlots: true);

            return true;

            void PrepareSelector()
            {
                if (!inputPrepared)
                {
                    Expression(selector);
                    RememberRecordAccesses(selector);
                }
            }
        }

        private bool HasLocalTail(Expr body)
        {
            Expr tail = UnwrapWildcardMatch(body is Expr.Sequence sequence ? sequence.Items[^1] : body);

            return tail is Expr.Call { Module: null, DynamicRemote: false } call
                && entries.ContainsKey((call.Function, call.Arguments.Count));
        }

        private static Integer? ReadSingletonInteger(Pattern pattern)
        {
            Integer? key = pattern switch
            {
                Pattern.Literal { Value: Cons { Head: Integer literal, Tail: Nil } } => literal,
                Pattern.List { Items: [Pattern.Literal { Value: Integer item }], Tail: null } => item,
                _ => null
            };

            return key is not null && key.Value >= int.MinValue && key.Value <= int.MaxValue ? key : null;
        }
    }
}
