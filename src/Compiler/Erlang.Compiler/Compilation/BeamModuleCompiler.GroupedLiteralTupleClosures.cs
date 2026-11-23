using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record LiteralTupleClosureArm(IReadOnlyList<Term> Fields, Expr.Fun Body, int Ordinal);

        private sealed record LiteralTupleClosureArity(int Size, int Selector, LiteralTupleClosureArm[] Arms);

        private bool TryEmitGroupedLiteralTupleClosures(FunctionDefinition function, int functionInfo)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Arity != 1
                || function.Clauses.Count < 3 || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding],
                    Body: Expr.Fun { SelfName: null } fallback
                })
            {
                return false;
            }

            var available = new Dictionary<string, Term>(StringComparer.Ordinal) { [binding.Name] = Nil.Value };
            if (ClosureCaptures.Capture(fallback, available).Count != 1 || HasNestedDefinitions(fallback))
            {
                return false;
            }

            var candidates = new List<LiteralTupleClosureArm>();
            for (int ordinal = 0; ordinal < function.Clauses.Count - 1; ordinal++)
            {
                if (function.Clauses[ordinal] is not
                    {
                        Guard: null, Patterns: [Pattern.Tuple tuple], Body: Expr.Fun { SelfName: null } closure
                    }
                    || tuple.Items.Count < 2 || HasNestedDefinitions(closure)
                    || tuple.Items.Any(item => item is not Pattern.Literal { Value: Integer }))
                {
                    return false;
                }

                candidates.Add(new(tuple.Items.Cast<Pattern.Literal>().Select(item => item.Value).ToArray(), closure, ordinal));
            }

            var arities = new List<LiteralTupleClosureArity>();
            foreach (var group in candidates.GroupBy(arm => arm.Fields.Count).OrderByDescending(group => group.Key))
            {
                LiteralTupleClosureArm[] arms = group.ToArray();
                int selector = -1;
                if (arms.Length > 1)
                {
                    var different = Enumerable.Range(0, group.Key)
                        .Where(field => arms.Skip(1).Any(arm => !arm.Fields[field].Equals(arms[0].Fields[field]))).ToArray();
                    if (different is not [var index] || arms.Select(arm => arm.Fields[index]).Distinct().Count() != arms.Length)
                    {
                        return false;
                    }

                    selector = index;
                    arms = arms.OrderByDescending(arm => arm.Fields[selector], Comparer<Term>.Create(
                        (left, right) => TermOrder.Compare(left, right, exact: true)
                    )).ToArray();
                }

                arities.Add(new(group.Key, selector, arms));
            }

            if (arities.Count < 2)
            {
                return false;
            }

            // Match cons/tuple type selection keeps the original tuple live
            // for the captured fallback. Each arity branches independently.
            // Within an arity, only the changing literal needs a value switch.
            var groups = arities.Select(group => (
                Plan: group,
                Entry: Label(),
                Targets: group.Selector < 0 ? Array.Empty<int>() : group.Arms.Select(_ => Label()).ToArray()
            )).ToArray();
            int failure = Label();
            owner.EmitTupleArityDispatch(failure, groups.Reverse().Select(group => (group.Plan.Size, group.Entry)).ToArray());

            variables = new(StringComparer.Ordinal);
            foreach (var (group, entry, targets) in groups)
            {
                Mark(entry);
                for (int field = 0; field < group.Size; field++)
                {
                    if (field == group.Selector)
                    {
                        continue;
                    }

                    owner.EmitTuplePart(0, field, 1);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(group.Arms[0].Fields[field]);
                }

                if (group.Selector < 0)
                {
                    EmitArm(group.Arms[0]);

                    continue;
                }

                owner.EmitTuplePart(0, group.Selector, 1);
                EmitAritySelect(1, targets.Length, failure);
                for (int arm = targets.Length - 1; arm >= 0; arm--)
                {
                    owner.LiteralOperand(group.Arms[arm].Fields[group.Selector]);
                    owner.Operand(LabelTag, targets[arm]);
                }

                for (int arm = 0; arm < targets.Length; arm++)
                {
                    Mark(targets[arm]);
                    EmitArm(group.Arms[arm]);
                }
            }

            Mark(failure);
            var captures = new Dictionary<string, int>(StringComparer.Ordinal) { [binding.Name] = 0 };
            EmitGeneralClosure(fallback with { CoreOrdinal = fallback.CoreOrdinal ?? function.Clauses.Count - 1 },
                captureRegisters: captures);
            owner.instructions.WriteByte(Return);

            return true;

            static bool HasNestedDefinitions(Expr.Fun closure) =>
                closure.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body))
                    .Any(expr => expr is Expr.Fun or Expr.ListComprehension or Expr.MapComprehension or Expr.BinaryComprehension);

            void EmitArm(LiteralTupleClosureArm arm)
            {
                EmitGeneralClosure(arm.Body with { CoreOrdinal = arm.Body.CoreOrdinal ?? arm.Ordinal });
                owner.instructions.WriteByte(Return);
            }
        }
    }
}
