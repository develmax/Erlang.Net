using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedStateEntryArm(
            Clause Clause,
            Atom StateTag,
            Dictionary<string, (int Input, int? Field)> Bindings,
            List<(int Input, int Arity, Atom Tag)> Checks
        );

        private bool TryEmitTaggedStateTupleGroup(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            ref int nextFrameKey
        )
        {
            int arity = clauses[0].Patterns.Count;
            int state = input + 1;
            int scratch = arity;
            if (clauses.Count != 2 || state >= arity || scratch + 1 >= FirstReservedXRegister
                || !owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null || owner.OnLoadFunction is not null)
            {
                return false;
            }

            Atom? messageTag = null;
            Atom? role = null;
            var arms = new List<TaggedStateEntryArm>();
            foreach (Clause clause in clauses)
            {
                if (clause.Guard is not null
                    || clause.Patterns[state] is not Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom stateTag }, Pattern.Literal { Value: Atom side }]
                    }
                    || role is not null && !role.Equals(side)
                    || LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                        expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe or Expr.Fun)
                    || ReadArm(clause, stateTag) is not { } arm)
                {
                    return false;
                }

                role ??= side;
                arms.Add(arm);
            }

            if (arms[0].StateTag.Equals(arms[1].StateTag))
            {
                return false;
            }

            int previousFrame = frameSize;
            int previousPeak = peakSlot;
            int previousSlot = nextSlot;
            var previousVariables = variables;
            var previousRecords = checkedRecordBindings;

            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var destinations = arms.OrderByDescending(arm => arm.StateTag, atomOrder)
                .Select(arm => (Arm: arm, Label: Label())).ToArray();

            owner.EmitTuplePart(input, 0, scratch);
            owner.EmitAtomTest(scratch, messageTag!.Name, failure);
            EmitTupleShapeTest(
                XRegisterTag,
                state,
                2,
                failure
            );
            owner.EmitTuplePart(state, 0, scratch);
            owner.EmitTuplePart(state, 1, scratch + 1);
            owner.EmitMembershipSelect(
                scratch,
                failure,
                destinations.Reverse().Select(item => ((Term)item.Arm.StateTag, item.Label))
            );

            foreach (var (arm, target) in destinations)
            {
                Mark(target);
                owner.EmitAtomTest(scratch + 1, role!.Name, failure);
                foreach (var (register, width, tag) in arm.Checks)
                {
                    EmitDetailRecordTest(
                        register,
                        width,
                        tag,
                        failure
                    );
                }

                int frameKey = nextFrameKey--;
                frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, previousFrame) ?? previousFrame;
                nextSlot = 0;
                peakSlot = 0;
                variables = new(StringComparer.Ordinal);
                checkedRecordBindings = new(StringComparer.Ordinal);

                if (!TryEmitSingleEntryEffect(arm.Clause, arm.Bindings))
                {
                    var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
                    int projection = scratch;
                    foreach (var (name, (source, field)) in arm.Bindings)
                    {
                        int targetRegister = source;
                        if (field is int position)
                        {
                            targetRegister = projection++;
                            owner.EmitTuplePart(source, position, targetRegister);
                        }

                        inputs.Add(name, targetRegister);
                    }

                    EmitNestedTupleLeaf(
                        new(
                            arm.Clause,
                            0,
                            arm.StateTag,
                            inputs,
                            [],
                            null
                        ),
                        frameSize,
                        recordFailures,
                        checkedPatterns: true
                    );
                }

                frames.Add(frameKey, peakSlot);
                previousPeak = Math.Max(previousPeak, peakSlot);
            }

            frameSize = previousFrame;
            nextSlot = previousSlot;
            peakSlot = previousPeak;
            variables = previousVariables;
            checkedRecordBindings = previousRecords;

            return true;

            TaggedStateEntryArm? ReadArm(Clause clause, Atom stateTag)
            {
                var bindings = new Dictionary<string, (int Input, int? Field)>(StringComparer.Ordinal);
                var checks = new List<(int Input, int Arity, Atom Tag)>();
                for (int register = 0; register < arity; register++)
                {
                    if (register == state)
                    {
                        continue;
                    }

                    if (!ReadPattern(clause.Patterns[register], register))
                    {
                        return null;
                    }
                }

                var parameters = bindings.Keys.Select(name => new Pattern.Variable(name)).ToArray();
                if (ReadUsedEntryParameters(clause with { Patterns = parameters }) is not { } used)
                {
                    return null;
                }

                var live = bindings.Where(binding => used.Contains(binding.Key))
                    .ToDictionary(binding => binding.Key, binding => binding.Value, StringComparer.Ordinal);

                return scratch + live.Count(binding => binding.Value.Field is not null) >= FirstReservedXRegister
                    ? null
                    : new(
                        clause,
                        stateTag,
                        live,
                        checks
                    );

                bool ReadPattern(Pattern pattern, int register)
                {
                    if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                        && shape is Pattern.Tuple)
                    {
                        if (!Bind(binding, register, null))
                        {
                            return false;
                        }

                        pattern = shape;
                    }

                    if (pattern is not Pattern.Tuple tuple)
                    {
                        return register != input && Bind(pattern, register, null);
                    }

                    if (!HasTupleTag(tuple, out Atom tag))
                    {
                        return false;
                    }

                    if (register == input)
                    {
                        if (messageTag is not null && !messageTag.Equals(tag))
                        {
                            return false;
                        }

                        messageTag ??= tag;
                    }
                    else if (register > state)
                    {
                        checks.Add((register, tuple.Items.Count, tag));
                    }
                    else
                    {
                        return false;
                    }

                    for (int field = 1; field < tuple.Items.Count; field++)
                    {
                        if (!Bind(tuple.Items[field], register, field))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                bool Bind(Pattern pattern, int register, int? field) => pattern switch
                {
                    Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard } => true,
                    Pattern.Variable variable => bindings.TryAdd(variable.Name, (register, field)),
                    _ => false
                };
            }
        }
    }
}
