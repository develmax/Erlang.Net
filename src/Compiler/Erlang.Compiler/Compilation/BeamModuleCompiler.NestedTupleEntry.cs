using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record NestedTupleProjection(int Source, int Field, int Destination);

        private sealed record NestedTupleLeaf(
            Clause Clause,
            int Arity,
            Atom Tag,
            Dictionary<string, int> Inputs,
            NestedTupleProjection[] Projections,
            ForwardGuard? Guard
        );

        private bool TryEmitNestedTupleGroup(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            ref int nextFrameKey
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || clauses.Count < 2
                || clauses[0].Patterns[input] is not Pattern.Tuple outer || !HasTupleTag(outer, out Atom outerTag))
            {
                return false;
            }

            int field = Array.FindIndex(outer.Items.ToArray(), item => item is Pattern.Tuple);
            if (field < 1)
            {
                return false;
            }

            int tupleRegister = clauses[0].Patterns.Count;
            var leaves = new List<NestedTupleLeaf>();
            foreach (Clause clause in clauses)
            {
                if (clause.Patterns[input] is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag)
                    || tag.Name != outerTag.Name || tuple.Items[field] is not Pattern.Tuple nested
                    || !HasTupleTag(nested, out Atom nestedTag))
                {
                    return false;
                }

                var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
                var projections = new List<NestedTupleProjection>();
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    if (index != input && !Bind(clause.Patterns[index], index))
                    {
                        return false;
                    }
                }

                for (int index = 1; index < tuple.Items.Count; index++)
                {
                    if (index != field && !Project(tuple.Items[index], input, index))
                    {
                        return false;
                    }
                }

                for (int index = 1; index < nested.Items.Count; index++)
                {
                    if (!Project(nested.Items[index], tupleRegister, index))
                    {
                        return false;
                    }
                }

                if (tupleRegister + projections.Count + 1 >= FirstReservedXRegister)
                {
                    return false;
                }

                ForwardGuard? guard = clause.Guard is null ? null : ReadGuard(clause.Guard);
                if (clause.Guard is not null && guard is null)
                {
                    return false;
                }

                leaves.Add(new NestedTupleLeaf(
                    clause,
                    nested.Items.Count,
                    nestedTag,
                    bindings,
                    projections.ToArray(),
                    guard
                ));

                bool Bind(Pattern pattern, int register) => pattern switch
                {
                    Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard } => true,
                    Pattern.Variable variable => bindings.TryAdd(variable.Name, register),
                    _ => false
                };

                bool Project(Pattern pattern, int source, int index)
                {
                    if (pattern is Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard })
                    {
                        return true;
                    }

                    int register = tupleRegister + projections.Count + 1;
                    if (!Bind(pattern, register))
                    {
                        return false;
                    }

                    projections.Add(new NestedTupleProjection(source, index, register));

                    return true;
                }

                ForwardGuard? ReadGuard(Expr expression)
                {
                    if (expression is Expr.Binary { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual } equality
                        && (IsNilComparison(equality.Left, equality.Right) ?? IsNilComparison(equality.Right, equality.Left)) is { } nil)
                    {
                        return nil;
                    }

                    return ReadForwardGuard(expression, NestedTupleInputNames(bindings));
                }

                ForwardGuard? IsNilComparison(Expr value, Expr literal) =>
                    value is Expr.Variable variable && bindings.TryGetValue(variable.Name, out int register)
                        && AttributeLiteral.TryNormalizeConstant(literal, out Term constant) && constant is Nil
                        ? new ForwardGuard.Test(IsNil, register)
                        : null;
            }

            var arities = leaves.GroupBy(leaf => leaf.Arity).OrderByDescending(group => group.Key).ToArray();
            var outerFields = leaves.SelectMany(leaf => leaf.Projections).Where(projection => projection.Source == input)
                .Select(projection => projection.Field).Distinct().Order().ToArray();
            if (arities.Length < 2 || tupleRegister + outerFields.Length + arities.Max(group => group.Key) >= FirstReservedXRegister)
            {
                return false;
            }

            int entryFrame = frameSize;
            int maxSlots = peakSlot;
            var arityLabels = arities.Select(group => (Arity: group.Key, Label: Label())).Reverse().ToArray();
            var outerRegisters = outerFields.Select((index, position) => (index, Register: tupleRegister + position + 1))
                .ToDictionary(field => field.index, field => field.Register);
            owner.EmitTuplePart(input, 0, tupleRegister);
            foreach (var (index, register) in outerRegisters)
            {
                owner.EmitTuplePart(input, index, register);
            }

            owner.EmitAtomTest(tupleRegister, outerTag.Name, failure);
            owner.EmitTuplePart(input, field, tupleRegister);
            owner.EmitTupleArityDispatch(failure, arityLabels, tupleRegister);

            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            for (int arityIndex = 0; arityIndex < arities.Length; arityIndex++)
            {
                Mark(arityLabels[^(arityIndex + 1)].Label);
                var tags = arities[arityIndex].GroupBy(leaf => leaf.Tag).OrderBy(group => group.Key, atomOrder).ToArray();
                int tagRegister = arities[arityIndex].SelectMany(leaf => leaf.Projections)
                    .Where(projection => projection.Source == input)
                    .Select(projection => outerRegisters[projection.Field]).DefaultIfEmpty(tupleRegister).Max() + 1;
                if (tags.Length == 1)
                {
                    owner.EmitTuplePart(tupleRegister, 0, tagRegister);
                }

                var innerRegisters = tags.Length == 1 ? new Dictionary<int, int>() :
                    EmitSharedNestedTupleProjections(arities[arityIndex], tupleRegister, tagRegister);
                int[] tagLabels = tags.Length == 1 ? [] : tags.Select(_ => Label()).Reverse().ToArray();
                if (tagLabels.Length == 0)
                {
                    owner.EmitAtomTest(tagRegister, tags[0].Key.Name, failure);
                }
                else
                {
                    EmitAritySelect(tagRegister, tags.Length, failure);
                    for (int index = 0; index < tags.Length; index++)
                    {
                        owner.Operand(AtomTag, owner.Intern(tags[index].Key.Name));
                        owner.Operand(LabelTag, tagLabels[index]);
                    }
                }

                for (int index = tags.Length - 1; index >= 0; index--)
                {
                    if (tagLabels.Length != 0)
                    {
                        Mark(tagLabels[index]);
                    }

                    foreach (NestedTupleLeaf sourceLeaf in tags[index])
                    {
                        NestedTupleLeaf leaf = PlaceSharedNestedTupleInputs(
                            sourceLeaf,
                            input,
                            outerRegisters,
                            innerRegisters
                        );
                        int retry = leaf.Guard is null ? 0 : Label();
                        bool projectionReturn = leaf.Guard is null && TryEmitLateNestedProjectionReturn(leaf);
                        foreach (NestedTupleProjection projection in projectionReturn ? [] : leaf.Projections)
                        {
                            owner.EmitTuplePart(projection.Source, projection.Field, projection.Destination);
                        }

                        if (leaf.Guard is not null)
                        {
                            EmitForwardGuard(leaf.Guard, retry);
                        }

                        nextSlot = 0;
                        peakSlot = 0;
                        variables = new(StringComparer.Ordinal);
                        int frameKey = nextFrameKey--;
                        frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, entryFrame) ?? entryFrame;
                        if (!projectionReturn)
                        {
                            EmitNestedTupleLeaf(leaf, frameSize, recordFailures);
                        }

                        frames.Add(frameKey, peakSlot);
                        maxSlots = Math.Max(maxSlots, peakSlot);

                        if (retry == 0)
                        {
                            break;
                        }

                        Mark(retry);
                        if (ReferenceEquals(sourceLeaf, tags[index].Last()))
                        {
                            JumpTo(failure);
                        }
                    }
                }
            }

            frameSize = entryFrame;
            peakSlot = maxSlots;

            return true;
        }

        private static string[] NestedTupleInputNames(Dictionary<string, int> inputs)
        {
            string[] names = Enumerable.Repeat(VariableScopeNames.Wildcard, inputs.Values.DefaultIfEmpty(-1).Max() + 1).ToArray();
            foreach (var (name, register) in inputs)
            {
                names[register] = name;
            }

            return names;
        }

        private void EmitNestedTupleLeaf(
            NestedTupleLeaf leaf,
            int slots,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            bool checkedPatterns = false
        )
        {
            Expr body = leaf.Clause.Body;
            if (AttributeLiteral.TryNormalizeConstant(body, out Term literal))
            {
                owner.EmitConstant(literal, 0);
                owner.instructions.WriteByte(Return);

                return;
            }

            if (body is Expr.Variable value && leaf.Inputs.TryGetValue(value.Name, out int source))
            {
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    XRegisterTag,
                    0
                );
                owner.instructions.WriteByte(Return);

                return;
            }

            if (ReadCallArgumentConstructions(
                [body],
                -1,
                variables,
                inputs: leaf.Inputs
            ) is { } construction)
            {
                EmitCallArgumentConstructions(construction, -1);
                owner.instructions.WriteByte(Return);

                return;
            }

            string[] parameters = NestedTupleInputNames(leaf.Inputs);
            if (body is Expr.Call invocation && ReadEntryCall(invocation, parameters) is { } tail)
            {
                EmitEntryCallArguments(tail);
                EmitRegisterTailTarget(tail.Call with { Module = tail.Module }, tail.Call.Arguments.Count);

                return;
            }

            var prepared = leaf.Clause with
            {
                Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray(),
                Guard = null
            };
            if (TryEmitCheckedResultRecordContinuation(prepared, recordFailures)
                || TryEmitGuardedTupleContinuation(prepared, parameters, leaf.Inputs.Keys))
            {
                return;
            }

            if (ReadEntryRegisterCase(prepared) is { } selector)
            {
                EmitEntryRegisterCase(selector);

                return;
            }

            var used = ReadUsedEntryParameters(prepared);
            var saved = leaf.Inputs.Where(binding => used is null || used.Contains(binding.Key)).ToArray();
            int live = saved.Select(binding => binding.Value).DefaultIfEmpty(-1).Max() + 1;
            EmitFrameAllocation(live, slots);
            EmitFrameInitialization(saved.Length, slots);
            foreach (var (name, register) in saved)
            {
                int slot = Slot();
                variables.Add(name, slot);
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    slot
                );
            }

            if (checkedPatterns)
            {
                foreach (Pattern pattern in leaf.Clause.Patterns)
                {
                    RememberEntryPatternRecords(pattern);
                }
            }

            if (!TailLocal(body))
            {
                Expression(body);
                owner.EmitFrameReturn(slots);
            }
        }
    }
}
