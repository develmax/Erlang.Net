using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TupleProjectionStep(int Arity, Atom? Tag, int Field);

        private bool TryEmitCallTupleCaseProjection(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call producer, Clauses.Count: > 0 } decision)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(producer, parameters) is not { } call)
            {
                return false;
            }

            var paths = new List<TupleProjectionStep[]>();
            Term? fallback = null;
            for (int index = 0; index < decision.Clauses.Count; index++)
            {
                Clause arm = decision.Clauses[index];
                if (arm is { Guard: null, Patterns: [Pattern.Tuple pattern], Body: Expr.Variable returned }
                    && ReadTupleProjection(pattern, returned.Name, parameters) is { } path)
                {
                    paths.Add(path);
                }
                else if (index == decision.Clauses.Count - 1 && arm.Guard is null
                    && arm.Patterns is [Erlang.Pattern.Any] or [Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    && AttributeLiteral.TryNormalizeConstant(arm.Body, out Term value))
                {
                    fallback = value;
                }
                else
                {
                    return false;
                }
            }

            if (paths.Count == 0)
            {
                return false;
            }

            var root = paths[0][0];
            if (paths.Count > 1 && (paths.Any(path => path[0].Arity != root.Arity
                    || path[0].Field != root.Field || path[0].Tag is null)
                || paths.Select(path => path[0].Tag).Distinct().Count() != paths.Count))
            {
                return false;
            }

            EmitFrameAllocation(EntryCallLiveCount(call, parameters), 0);
            EmitEntryCall(call);
            int selectorType = RegisterCaseSelectorType(call, IsTuple);
            if (paths.Count == 1)
            {
                int failure = Label();
                int register = fallback is null ? 1 : 0;
                EmitTupleProjection(paths[0], 0, register, failure, selectorType);
                EmitFallback(failure);
            }
            else
            {
                // Leave the original selector in X0 for any failing path.
                const int payloadRegister = 2;
                var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                var ordered = paths.OrderByDescending(path => path[0].Tag!, atomOrder).ToArray();
                int[] labels = ordered.Select(_ => Label()).ToArray();
                int failure = Label();
                EmitTupleShapeTest(XRegisterTag, 0, root.Arity, failure, selectorType);
                owner.EmitTuplePart(0, 0, 1);
                owner.EmitTuplePart(0, root.Field, payloadRegister);
                owner.EmitMembershipSelect(
                    1,
                    failure,
                    ordered.Select((path, index) => ((Term)path[0].Tag!, labels[index])).Reverse()
                );
                for (int index = 0; index < ordered.Length; index++)
                {
                    Mark(labels[index]);
                    var path = ordered[index];
                    if (path.Length == 1)
                    {
                        owner.MoveRegister(XRegisterTag, payloadRegister, XRegisterTag, 0);
                        owner.EmitFrameReturn();
                    }
                    else
                    {
                        EmitTupleProjection(path.Skip(1), payloadRegister, payloadRegister, failure, 0);
                    }
                }

                EmitFallback(failure);
            }

            return true;

            void EmitFallback(int failure)
            {
                Mark(failure);
                if (fallback is null)
                {
                    owner.Op(CaseEnd, XRegisterTag, 0);
                }
                else
                {
                    owner.EmitConstant(fallback, 0);
                    owner.EmitFrameReturn();
                }
            }
        }

        private static TupleProjectionStep[]? ReadTupleProjection(
            Pattern.Tuple root,
            string returned,
            string[] parameters
        )
        {
            if (returned == VariableScopeNames.Wildcard)
            {
                return null;
            }

            string[] bindings = Semantics.Variables(root).ToArray();
            if (bindings.Intersect(parameters, StringComparer.Ordinal).Any()
                || bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Length)
            {
                return null;
            }

            var steps = new List<TupleProjectionStep>();
            Pattern.Tuple tuple = root;
            while (true)
            {
                Atom? tag = tuple.Items.FirstOrDefault() is Pattern.Literal { Value: Atom atom } ? atom : null;
                int field = -1;
                Pattern? member = null;
                for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
                {
                    Pattern item = tuple.Items[index];
                    if (item is Erlang.Pattern.Any || item is Pattern.Variable name && name.Name != returned)
                    {
                        continue;
                    }

                    if (field >= 0 || item is not (Erlang.Pattern.Tuple or Erlang.Pattern.Variable))
                    {
                        return null;
                    }

                    field = index;
                    member = item;
                }

                if (field < 0)
                {
                    return null;
                }

                steps.Add(new TupleProjectionStep(tuple.Items.Count, tag, field));
                if (member is Erlang.Pattern.Variable)
                {
                    return steps.ToArray();
                }

                tuple = (Pattern.Tuple)member!;
            }
        }

        private void EmitTupleProjection(
            IEnumerable<TupleProjectionStep> path,
            int input,
            int scratch,
            int failure,
            int selectorType
        )
        {
            TupleProjectionStep[] steps = path.ToArray();
            for (int index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step.Tag is { } tag)
                {
                    if (selectorType != 0)
                    {
                        owner.Op(IsTuple, LabelTag, failure);
                        owner.EmitTypedRegister(input, selectorType);
                    }

                    EmitDetailRecordTest(input, step.Arity, tag, failure, typedTuple: selectorType != 0);
                }
                else
                {
                    EmitTupleShapeTest(XRegisterTag, input, step.Arity, failure, selectorType);
                }

                owner.EmitTuplePart(input, step.Field, index == steps.Length - 1 ? 0 : scratch);
                input = scratch;
                selectorType = 0;
            }

            owner.EmitFrameReturn();
        }
    }
}
