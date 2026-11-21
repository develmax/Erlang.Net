using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedEntryField(int Index, string? Binding, Atom? Literal);

        private sealed record TaggedEntryArm(
            Clause Clause,
            Atom Tag,
            Dictionary<string, int> Arguments,
            TaggedEntryField[] Fields
        );

        private bool TryEmitFlatTaggedTupleGroup(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            ref int nextFrameKey
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || clauses.Count < 2)
            {
                return false;
            }

            int tagRegister = clauses[0].Patterns.Count;
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var arms = new SortedDictionary<Atom, TaggedEntryArm>(order);
            foreach (Clause clause in clauses)
            {
                if (ReadFlatTaggedEntryArm(clause, input) is not { } arm
                    || !arms.TryAdd(arm.Tag, arm))
                {
                    return false;
                }
            }

            int[] shared = arms.Values.First().Fields.Select(field => field.Index)
                .Where(index => arms.Values.All(arm => arm.Fields.Any(field => field.Index == index)))
                .ToArray();
            if (shared.Length == 0
                || tagRegister + arms.Values.Max(arm => arm.Fields.Length) >= FirstReservedXRegister)
            {
                return false;
            }

            int entryFrame = frameSize;
            int maxSlots = peakSlot;
            int[] labels = arms.Values.Select(_ => Label()).Reverse().ToArray();

            // The enclosing arity dispatch has already proved that every
            // source field exists. Keep the original input for other tags.
            owner.EmitTuplePart(input, 0, tagRegister);
            var sharedRegisters = new Dictionary<int, int>();
            foreach (int index in shared)
            {
                int register = tagRegister + sharedRegisters.Count + 1;
                sharedRegisters.Add(index, register);
                owner.EmitTuplePart(input, index, register);
            }

            EmitAritySelect(tagRegister, arms.Count, failure);
            int branch = 0;
            foreach (Atom tag in arms.Keys)
            {
                owner.Operand(AtomTag, owner.Intern(tag.Name));
                owner.Operand(LabelTag, labels[branch++]);
            }

            foreach (var arm in arms.Values.Reverse())
            {
                Mark(labels[--branch]);
                int nextRegister = tagRegister;
                foreach (TaggedEntryField field in arm.Fields.Where(field => field.Literal is not null))
                {
                    if (sharedRegisters.TryGetValue(field.Index, out int value))
                    {
                        owner.EmitAtomTest(value, field.Literal!.Name, failure);
                        continue;
                    }

                    owner.EmitTuplePart(input, field.Index, nextRegister);
                    owner.EmitAtomTest(nextRegister, field.Literal!.Name, failure);
                }

                nextSlot = 0;
                peakSlot = 0;
                variables = new(StringComparer.Ordinal);
                int frameKey = nextFrameKey--;
                frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, entryFrame) ?? entryFrame;
                var prepared = arm.Arguments.ToDictionary(
                    binding => binding.Key,
                    binding => (binding.Value, (int?)null),
                    StringComparer.Ordinal
                );
                foreach (TaggedEntryField field in arm.Fields.Where(field => field.Binding is not null))
                {
                    prepared.Add(field.Binding!, sharedRegisters.TryGetValue(field.Index, out int register)
                        ? (register, null) : (input, field.Index));
                }

                if (TryEmitSingleEntryEffect(arm.Clause, prepared))
                {
                    frames.Add(frameKey, peakSlot);
                    continue;
                }

                foreach (TaggedEntryField field in arm.Fields.Where(field => field.Binding is not null))
                {
                    if (!sharedRegisters.TryGetValue(field.Index, out int value))
                    {
                        value = nextRegister;
                        nextRegister = nextRegister == tagRegister
                            ? tagRegister + sharedRegisters.Count + 1 : nextRegister + 1;
                        owner.EmitTuplePart(input, field.Index, value);
                    }

                    arm.Arguments.Add(field.Binding!, value);
                }

                EmitNestedTupleLeaf(
                    new NestedTupleLeaf(
                        arm.Clause,
                        0,
                        arm.Tag,
                        arm.Arguments,
                        [],
                        null
                    ),
                    frameSize,
                    recordFailures
                );
                frames.Add(frameKey, peakSlot);
                maxSlots = Math.Max(maxSlots, peakSlot);
            }

            frameSize = entryFrame;
            peakSlot = maxSlots;

            return true;
        }

        private static TaggedEntryArm? ReadFlatTaggedEntryArm(Clause clause, int input, string? required = null)
        {
            if (clause.Guard is not null || clause.Patterns[input] is not Pattern.Tuple tuple
                || !HasTupleTag(tuple, out Atom tag))
            {
                return null;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int register = 0; register < clause.Patterns.Count; register++)
            {
                if (register == input)
                {
                    continue;
                }

                if (clause.Patterns[register] is not Pattern.Variable parameter)
                {
                    return null;
                }

                if (parameter.Name != VariableScopeNames.Wildcard)
                {
                    if (!names.Add(parameter.Name))
                    {
                        return null;
                    }

                    inputs.Add(parameter.Name, register);
                }
            }

            var fields = new List<TaggedEntryField>();
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when names.Add(variable.Name):
                        fields.Add(new(index, variable.Name, null));
                        break;
                    case Pattern.Literal { Value: Atom literal }:
                        fields.Add(new(index, null, literal));
                        break;
                    default:
                        return null;
                }
            }

            var tracked = names.Select(name => (Pattern)new Pattern.Variable(name)).ToArray();
            if (ReadUsedEntryParameters(clause with { Patterns = tracked }) is not { } used)
            {
                return null;
            }

            return new(
                clause,
                tag,
                inputs,
                fields.Where(field => field.Binding is null || field.Binding == required || used.Contains(field.Binding)).ToArray()
            );
        }
    }
}
