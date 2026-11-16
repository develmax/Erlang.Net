using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleGroupsWithDynamicCase(IGrouping<int, Clause>[] groups, Clause fallback)
        {
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int register = 0; register < fallback.Patterns.Count; register++)
            {
                if (fallback.Patterns[register] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && !inputs.TryAdd(variable.Name, register))
                {
                    return false;
                }
            }

            if (fallback.Body is not Expr.Call tail
                || !TryEmitRegisterConstructedArgument(
                    tail,
                    inputs,
                    validateOnly: true,
                    allowControlTransfer: true
                ))
            {
                return false;
            }

            var plans = new Action<int, int>[groups.Length];
            bool dynamic = false;
            for (int index = 0; index < groups.Length; index++)
            {
                Clause[] clauses = groups[index].ToArray();
                if (clauses is [var clause] && ReadTupleGroupDynamicCase(clause) is { } decision)
                {
                    plans[index] = decision;
                    dynamic = true;
                }
                else if (ReadTupleGroupConstructors(clauses) is { } constructors)
                {
                    plans[index] = (failure, _) => constructors(failure);
                }
                else
                {
                    return false;
                }
            }

            if (!dynamic)
            {
                return false;
            }

            const int pending = int.MinValue;
            int fallbackTarget = pending + groups.Length;
            int failureTarget = fallbackTarget + 1;
            int start = owner.deferredLabelOperands.Count;
            owner.EmitTupleArityDispatch(fallbackTarget, groups.Select((group, index) => (group.Key, pending + index)).ToArray());
            var labels = new Dictionary<int, int>();
            var caseFailures = new List<BeamSourceLocation>();
            for (int index = plans.Length - 1; index >= 0; index--)
            {
                Mark(labels[pending + index] = Label());
                int target = failureTarget + caseFailures.Count;
                plans[index](fallbackTarget, target);
                if (owner.deferredLabelOperands.Skip(start).Any(operand => operand.Target == target))
                {
                    var selector = (Expr.Call)((Expr.Case)groups[index].Single().Body).Value;
                    caseFailures.Add(CallLocation(selector, location));
                }
            }

            Mark(labels[fallbackTarget] = Label());
            TryEmitRegisterConstructedArgument(tail, inputs, allowControlTransfer: true);
            for (int index = 0; index < caseFailures.Count; index++)
            {
                Mark(labels[failureTarget + index] = Label());
                owner.EmitLine(caseFailures[index]);
                owner.Op(CaseEnd, XRegisterTag, 0);
            }

            for (int index = start; index < owner.deferredLabelOperands.Count; index++)
            {
                var (position, target) = owner.deferredLabelOperands[index];
                if (labels.TryGetValue(target, out int label))
                {
                    owner.deferredLabelOperands[index] = (position, label);
                }
            }

            return true;
        }

        private Action<int>? ReadTupleGroupConstructors(Clause[] clauses)
        {
            if (clauses.Length < 2)
            {
                return null;
            }

            var branches = new Dictionary<Atom, CallArgumentConstructions>();
            foreach (Clause clause in clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag },
                            Pattern.Variable { Name: not VariableScopeNames.Wildcard } field]
                        }, ..]
                    }
                    || clause.Patterns.Skip(1).Any(pattern => pattern is not Pattern.Variable { })
                    || clause.Patterns.Skip(1).OfType<Pattern.Variable>()
                        .Where(variable => variable.Name != VariableScopeNames.Wildcard)
                        .Select(variable => variable.Name).Append(field.Name).Distinct(StringComparer.Ordinal).Count()
                        != clause.Patterns.Skip(1).Count(pattern => pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard }) + 1
                    || ReadCallArgumentConstructions(
                        [clause.Body],
                        -1,
                        new Dictionary<string, int>(),
                        inputs: new Dictionary<string, int> { [field.Name] = 2 }
                    ) is not { Constructions: [{ IsTuple: true }] } constructor
                    || !branches.TryAdd(tag, constructor))
                {
                    return null;
                }
            }

            return failure =>
            {
                owner.EmitTuplePart(0, 0, 1);
                owner.EmitTuplePart(0, 1, 2);
                var ordered = branches.OrderByDescending(branch => branch.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)
                )).Select(branch => (branch.Key, branch.Value, Label: Label())).ToArray();
                owner.EmitMembershipSelect(1, failure, ordered.Reverse().Select(branch => ((Term)branch.Key, branch.Label)));
                foreach (var (_, constructor, label) in ordered)
                {
                    Mark(label);
                    EmitCallArgumentConstructions(constructor, -1);
                    owner.instructions.WriteByte(Return);
                }
            };
        }

        private Action<int, int>? ReadTupleGroupDynamicCase(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple tuple, ..],
                    Body: Expr.Case { Value: Expr.Call { DynamicRemote: false } selector, Clauses: [var tagged, _, ..] } decision
                }
                || clause.Patterns.Count + 2 >= FirstReservedXRegister || !HasTupleTag(tuple, out Atom entryTag)
                || tagged is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom resultTag },
                        Pattern.Variable { Name: not VariableScopeNames.Wildcard } result]
                    }],
                    Body: Expr.Call { DynamicRemote: true } taggedCall
                })
            {
                return null;
            }

            var bindings = new List<(Pattern.Variable Variable, int Index, bool Field)>();
            for (int index = 1; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is not Pattern.Variable variable)
                {
                    return null;
                }

                bindings.Add((variable, index, false));
            }

            for (int index = tuple.Items.Count - 1; index > 0; index--)
            {
                if (tuple.Items[index] is not Pattern.Variable variable)
                {
                    return null;
                }

                bindings.Add((variable, index, true));
            }

            bindings.RemoveAll(binding => binding.Variable.Name == VariableScopeNames.Wildcard);
            string[] names = bindings.Select(binding => binding.Variable.Name).ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length || names.Contains(result.Name, StringComparer.Ordinal)
                || ReadUsedEntryParameters(new Clause(bindings.Select(binding => (Pattern)binding.Variable).ToArray(), null, decision))
                    is not { } used)
            {
                return null;
            }

            bindings.RemoveAll(binding => !used.Contains(binding.Variable.Name));
            string[] slots = bindings.Select(binding => binding.Variable.Name).Reverse().ToArray();
            var frame = slots.Select((name, slot) => (name, slot)).ToDictionary(entry => entry.name, entry => entry.slot, StringComparer.Ordinal);
            if (bindings.FindIndex(binding => binding.Field) is not (>= 0 and var projected)
                || ReadEntryCall(selector, [], slots) is not { } prefix
                || ReadDynamicTail(taggedCall, slots, result.Name) is not { } taggedTail)
            {
                return null;
            }

            var previous = CallLocation(selector, location);
            var constructorFrame = ((IReadOnlyDictionary<string, int>)frame, slots.Length, previous, (IReadOnlySet<Expr>)new HashSet<Expr>());
            var branches = new Dictionary<Atom, Action>();
            foreach (Clause branch in decision.Clauses.Skip(1))
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call call })
                {
                    return null;
                }

                Action emit;
                if (call.DynamicRemote && ReadDynamicTail(call, slots) is { } dynamicTail)
                {
                    emit = () => EmitDynamic(dynamicTail);
                }
                else if (ReadEntryCall(call, [], slots) is { } ordinary)
                {
                    emit = () =>
                    {
                        owner.EmitOrderedTailMoves(ordinary.Moves, ordinary.Constants, ordinary.SavedInputs);
                        EmitPreparedFrameTail(ordinary, previous, slots.Length);
                    };
                }
                else if (TryEmitRegisterConstructedArgument(
                    call,
                    new Dictionary<string, int>(),
                    frame: constructorFrame,
                    validateOnly: true,
                    allowControlTransfer: true
                ))
                {
                    emit = () => TryEmitRegisterConstructedArgument(
                        call,
                        new Dictionary<string, int>(),
                        frame: constructorFrame,
                        allowControlTransfer: true
                    );
                }
                else
                {
                    return null;
                }

                if (!branches.TryAdd(key, emit))
                {
                    return null;
                }
            }

            return (failure, caseFailure) =>
            {
                int arity = clause.Patterns.Count;
                owner.EmitTuplePart(0, 0, arity);
                owner.EmitTuplePart(0, bindings[projected].Index, arity + 1);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, arity);
                owner.LiteralOperand(entryTag);
                EmitFrameAllocation(arity + 2, slots.Length);
                owner.MoveRegister(
                    XRegisterTag,
                    arity + 1,
                    YRegisterTag,
                    slots.Length - 1 - projected
                );
                for (int index = 0; index < bindings.Count; index++)
                {
                    if (index == projected)
                    {
                        continue;
                    }

                    var (_, source, isField) = bindings[index];
                    if (isField)
                    {
                        owner.Op(GetTupleElement, XRegisterTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, source);
                        owner.Operand(YRegisterTag, slots.Length - 1 - index);
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            source,
                            YRegisterTag,
                            slots.Length - 1 - index
                        );
                    }
                }

                owner.EmitOrderedTailMoves(prefix.Moves, prefix.Constants, prefix.SavedInputs);
                owner.EmitCallLine(prefix.Call, location);
                Call(prefix.Module, prefix.Call.Function, prefix.Call.Arguments.Count);
                int atoms = Label();
                owner.Op(IsTuple, LabelTag, atoms);
                owner.Operand(XRegisterTag, 0);
                EmitDetailRecordTest(
                    0,
                    2,
                    resultTag,
                    caseFailure,
                    TupleTypeBits
                );
                owner.EmitTuplePart(0, 1, 0);
                EmitDynamic(taggedTail);

                Mark(atoms);
                var ordered = branches.OrderByDescending(branch => branch.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)
                )).Select(branch => (branch.Key, branch.Value, Label: Label())).ToArray();
                owner.EmitMembershipSelect(0, caseFailure, ordered.Reverse().Select(branch => ((Term)branch.Key, branch.Label)));
                foreach (var (_, emit, label) in ordered)
                {
                    Mark(label);
                    emit();
                }
            };

            void EmitDynamic((EntryCall Plan, int Arity) dynamicTail)
            {
                var (plan, arity) = dynamicTail;
                if (plan.Constants.All(constant => constant.Destination != 0))
                {
                    owner.EmitIndependentCallMoves(plan.Constants);
                    owner.EmitOrderedTailMoves(plan.Moves, [], plan.SavedInputs);
                }
                else
                {
                    owner.EmitOrderedTailMoves(plan.Moves, plan.Constants, plan.SavedInputs);
                }
                owner.EmitCallLine(plan.Call, previous);
                owner.Op(TailDynamicApply, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, slots.Length);
            }
        }
    }
}
