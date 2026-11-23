using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ClosedTupleClosureDecision(
            int Label,
            int Field,
            IReadOnlyList<Term> Shared,
            LiteralTupleClosureArm? Leaf,
            IReadOnlyList<(Term Key, ClosedTupleClosureDecision Decision)> Branches
        );

        private bool TryEmitClosedLiteralTupleDecision(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Arity != 1 || function.Clauses.Count < 4
                || function.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable fallbackBinding], Body: Expr.Fun { SelfName: null } fallback
                }
                || HasNestedDefinitions(fallback))
            {
                return false;
            }

            bool capturesOriginal = fallbackBinding.Name != VariableScopeNames.Wildcard
                && ClosureCaptures.Capture(fallback, new Dictionary<string, Term>(StringComparer.Ordinal)
                { [fallbackBinding.Name] = Nil.Value }).Count != 0;
            var blocks = new List<List<LiteralTupleClosureArm>> { new() };
            var references = new List<(Term Key, Expr.FunctionReference Reference, int Entry)>();
            int ordinal = 0;
            var patterns = new HashSet<Term>();
            foreach (var clause in function.Clauses.Take(function.Clauses.Count - 1))
            {
                if (clause is
                    {
                        Patterns: [Pattern.Variable subject],
                        Guard: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual, Left: Expr.Variable candidate, Right: var literal
                        },
                        Body: Expr.FunctionReference reference
                    }
                    && subject.Name == candidate.Name && subject.Name != VariableScopeNames.Wildcard
                    && AttributeLiteral.TryNormalizeConstant(literal, out Term key)
                    && key is TupleTerm guardTuple && guardTuple.Items.Count > 0 && guardTuple.Items.All(field => field is Integer)
                    && entries.TryGetValue((reference.Name, reference.Arity), out int entry)
                    && blocks[^1].Count > 0 && patterns.Add(key))
                {
                    references.Add((key, reference, entry));
                    blocks.Add([]);

                    continue;
                }

                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Tuple tuple], Body: Expr.Fun { SelfName: null } closure
                    } || tuple.Items.Count is 0 or >= FirstReservedXRegister
                    || tuple.Items.Any(item => item is not Pattern.Literal { Value: Integer })
                    || HasNestedDefinitions(closure))
                {
                    return false;
                }

                Term[] fields = tuple.Items.Cast<Pattern.Literal>().Select(item => item.Value).ToArray();
                if (!patterns.Add(Term.Tuple(fields)))
                {
                    return false;
                }

                blocks[^1].Add(new(fields, closure, ordinal));
                ordinal++;
            }

            if (blocks[^1].Count == 0)
            {
                return false;
            }

            var byArity = blocks.Select(arms => arms.GroupBy(arm => arm.Fields.Count).OrderByDescending(group => group.Key).ToArray())
                .ToArray();
            if (references.Count == 0 && byArity[0] is [var onlyArity]
                && Enumerable.Range(0, onlyArity.Key)
                    .Count(field => onlyArity.Any(arm => !arm.Fields[field].Equals(onlyArity.First().Fields[field]))) < 2)
            {
                return false;
            }

            var order = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var schedules = byArity.Select((arities, index) =>
            {
                int entry = index == 0 ? 0 : Label();
                var groups = arities.Select(group => (Arity: group.Key, Decision: Plan(group.ToArray(), 0, arities.Length > 1)))
                    .ToArray();
                int reference = index < references.Count ? Label() : 0;

                return (Entry: entry, Groups: groups, Reference: reference);
            }).ToArray();
            int fallbackLabel = Label();
            variables = new(StringComparer.Ordinal);
            for (int index = 0; index < schedules.Length; index++)
            {
                var (entry, groups, referenceLabel) = schedules[index];
                if (entry != 0)
                {
                    Mark(entry);
                }

                int failure = referenceLabel != 0 ? referenceLabel : fallbackLabel;
                if (groups is [var single])
                {
                    owner.EmitTupleCheck(0, single.Arity, failure);
                }
                else
                {
                    owner.EmitTupleArityDispatch(failure, groups.Reverse().Select(group => (group.Arity, group.Decision.Label)).ToArray());
                }

                foreach (var (arity, decision) in groups)
                {
                    Emit(
                        decision,
                        arity,
                        preloaded: null,
                        failure,
                        preserveInput: referenceLabel != 0 || capturesOriginal
                    );
                }

                if (referenceLabel != 0)
                {
                    Mark(referenceLabel);
                    var (key, reference, target) = references[index];
                    owner.Op(ExactEqualTest, LabelTag, schedules[index + 1].Entry);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(key);
                    owner.EmitClosureAllocation(0, 0, 1);
                    owner.EmitDeclaredFunctionReference(reference, target, 0);
                    owner.instructions.WriteByte(Return);
                }
            }

            Mark(fallbackLabel);
            var captures = capturesOriginal
                ? new Dictionary<string, int>(StringComparer.Ordinal) { [fallbackBinding.Name] = 0 }
                : null;
            EmitGeneralClosure(fallback with { CoreOrdinal = fallback.CoreOrdinal ?? ordinal }, captureRegisters: captures);
            owner.instructions.WriteByte(Return);

            return true;

            ClosedTupleClosureDecision Plan(LiteralTupleClosureArm[] candidates, int first, bool hasLabel = true)
            {
                int label = hasLabel ? Label() : 0;
                var shared = new List<Term>();
                int field = first;
                while (field < candidates[0].Fields.Count && candidates.All(arm => arm.Fields[field].Equals(candidates[0].Fields[field])))
                {
                    shared.Add(candidates[0].Fields[field]);
                    field++;
                }

                if (field == candidates[0].Fields.Count)
                {
                    return new(
                        label,
                        field,
                        shared,
                        candidates[0],
                        []
                    );
                }

                var branches = candidates.GroupBy(arm => arm.Fields[field]).OrderByDescending(group => group.Key, order)
                    .Select(group => (group.Key, Plan(group.ToArray(), field + 1))).ToArray();

                return new(
                    label,
                    field,
                    shared,
                    null,
                    branches
                );
            }

            void Emit(
                ClosedTupleClosureDecision decision,
                int arity,
                int? preloaded,
                int failure,
                bool preserveInput
            )
            {
                if (decision.Label != 0)
                {
                    Mark(decision.Label);
                }
                int field = decision.Field - decision.Shared.Count;
                foreach (Term expected in decision.Shared)
                {
                    int register = Register(field, preloaded);
                    if (preloaded is null)
                    {
                        owner.EmitTuplePart(0, field, register);
                    }

                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, register);
                    owner.LiteralOperand(expected);
                    field++;
                }

                if (decision.Leaf is { } leaf)
                {
                    EmitGeneralClosure(leaf.Body with { CoreOrdinal = leaf.Body.CoreOrdinal ?? leaf.Ordinal });
                    owner.instructions.WriteByte(Return);

                    return;
                }

                if (preloaded is null)
                {
                    preloaded = field;
                    for (int index = field; index < arity; index++)
                    {
                        owner.EmitTuplePart(0, index, Register(index, preloaded));
                    }
                }

                EmitAritySelect(Register(field, preloaded), decision.Branches.Count, failure);
                foreach (var (key, branch) in decision.Branches.Reverse())
                {
                    owner.LiteralOperand(key);
                    owner.Operand(LabelTag, branch.Label);
                }

                foreach (var (_, branch) in decision.Branches)
                {
                    Emit(
                        branch,
                        arity,
                        preloaded,
                        failure,
                        preserveInput
                    );
                }

                int Register(int index, int? first) => !preserveInput && index == arity - 1 ? 0 : first is int loaded ? index - loaded + 1 : 1;
            }

        }

        private static bool HasNestedDefinitions(Expr.Fun closure) =>
            closure.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body))
                .Any(expression => expression is Expr.Fun or Expr.ListComprehension or Expr.MapComprehension or Expr.BinaryComprehension);
    }
}
