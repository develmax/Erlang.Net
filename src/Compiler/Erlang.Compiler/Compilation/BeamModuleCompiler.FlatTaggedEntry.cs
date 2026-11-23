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

        private sealed record RepeatedTaggedEntryBucket(int Field, Atom?[] Keys);

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

            var sourceTags = new HashSet<Atom>();
            bool hasRepeatedTags = false;
            foreach (Clause clause in clauses)
            {
                if (EntryShape(clause.Patterns[input]) is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag))
                {
                    return false;
                }

                hasRepeatedTags |= !sourceTags.Add(tag);
            }

            int tagRegister = clauses[0].Patterns.Count;
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var buckets = new SortedDictionary<Atom, List<TaggedEntryArm>>(order);
            foreach (Clause clause in clauses)
            {
                if (ReadFlatTaggedEntryArm(clause, input, allowCarrierAlias: hasRepeatedTags) is not { } arm)
                {
                    return false;
                }

                if (!buckets.TryGetValue(arm.Tag, out var bucket))
                {
                    bucket = [];
                    buckets.Add(arm.Tag, bucket);
                }

                bucket.Add(arm);
            }

            var repeated = new Dictionary<Atom, RepeatedTaggedEntryBucket>();
            foreach (var (tag, bucket) in buckets)
            {
                if (bucket.Count == 1)
                {
                    continue;
                }

                if (ReadRepeatedTaggedEntryBucket(bucket) is not { } selector)
                {
                    return false;
                }

                repeated.Add(tag, selector);
            }

            TaggedEntryArm[] arms = buckets.Values.SelectMany(bucket => bucket).ToArray();
            int[] shared = ReadSharedFlatTaggedFields(arms);
            if (buckets.Count < 2 || tagRegister + arms.Max(arm => arm.Fields.Length) >= FirstReservedXRegister)
            {
                return false;
            }

            // Distinct tags choose one leaf even when no data field is used in
            // every arm. Keep closure and record update ownership on the
            // existing entry path until their leaf plans can carry it.
            if (shared.Length == 0 && repeated.Count == 0 && clauses.Any(clause =>
                LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Fun or Expr.FunctionReference or Expr.RecordUpdate)))
            {
                return false;
            }

            int entryFrame = frameSize;
            int maxSlots = peakSlot;
            int nextKey = nextFrameKey;
            int[] labels = buckets.Values.Select(_ => Label()).Reverse().ToArray();

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

            EmitAritySelect(tagRegister, buckets.Count, failure);
            int branch = 0;
            foreach (Atom tag in buckets.Keys)
            {
                owner.Operand(AtomTag, owner.Intern(tag.Name));
                owner.Operand(LabelTag, labels[branch++]);
            }

            foreach (var (tag, bucket) in buckets.Reverse())
            {
                Mark(labels[--branch]);
                if (!repeated.TryGetValue(tag, out var selector))
                {
                    EmitArm(bucket[0], selectedField: -1);
                    continue;
                }

                int register = tagRegister;
                if (sharedRegisters.TryGetValue(selector.Field, out int sharedRegister))
                {
                    register = sharedRegister;
                }
                else
                {
                    owner.EmitTuplePart(input, selector.Field, register);
                }

                if (selector.Keys is [Atom retryKey, null])
                {
                    int retry = Label();
                    owner.EmitAtomTest(register, retryKey.Name, retry);
                    EmitArm(bucket[0], selector.Field);
                    Mark(retry);
                    EmitArm(bucket[1], selector.Field);
                    continue;
                }

                var targets = bucket.Select((arm, index) => (Arm: arm, Key: selector.Keys[index]!))
                    .OrderByDescending(item => item.Key, order)
                    .Select(item => (item.Arm, item.Key, Target: Label())).ToArray();
                EmitAritySelect(register, targets.Length, failure);
                foreach (var (_, key, target) in targets.Reverse())
                {
                    owner.Operand(AtomTag, owner.Intern(key.Name));
                    owner.Operand(LabelTag, target);
                }

                foreach (var (arm, _, target) in targets)
                {
                    Mark(target);
                    EmitArm(arm, selector.Field);
                }
            }

            frameSize = entryFrame;
            peakSlot = maxSlots;
            nextFrameKey = nextKey;

            return true;

            void EmitArm(TaggedEntryArm arm, int selectedField)
            {
                int nextRegister = tagRegister;
                foreach (TaggedEntryField field in arm.Fields.Where(field => field.Literal is not null && field.Index != selectedField))
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
                int frameKey = nextKey--;
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

                    return;
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
        }

        private static RepeatedTaggedEntryBucket? ReadRepeatedTaggedEntryBucket(IReadOnlyList<TaggedEntryArm> arms)
        {
            int[] fields = arms.SelectMany(arm => arm.Fields)
                .Where(field => field.Literal is not null).Select(field => field.Index).Distinct().Order().ToArray();
            if (fields.Length == 0)
            {
                return null;
            }

            // Only the first literal column can choose the retry. Every later
            // test must be common, so it cannot reopen a selected alternative.
            int selector = fields[0];
            Atom?[] keys = Column(selector);
            if (keys.Contains(null) && keys is not [Atom, null]
                || keys.Distinct().Count() != keys.Length
                || fields.Skip(1).Any(field => Column(field).Distinct().Count() != 1))
            {
                return null;
            }

            return new(selector, keys);

            Atom?[] Column(int index) => arms.Select(arm =>
                arm.Fields.FirstOrDefault(field => field.Index == index)?.Literal).ToArray();
        }

        private static bool CanUseCompleteSharedFlatTaggedGroup(IReadOnlyList<Clause> clauses, int input)
        {
            var arms = new List<TaggedEntryArm>();
            var tags = new HashSet<Atom>();
            foreach (Clause clause in clauses)
            {
                if (ReadFlatTaggedEntryArm(clause, input) is not { } arm
                    || !tags.Add(arm.Tag) || clause.Patterns.Count + arm.Fields.Length >= FirstReservedXRegister)
                {
                    return false;
                }

                arms.Add(arm);
            }

            return arms.Count >= 2 && ReadSharedFlatTaggedFields(arms).Length != 0;
        }

        private static int[] ReadSharedFlatTaggedFields(IReadOnlyList<TaggedEntryArm> arms) =>
            arms[0].Fields.Select(field => field.Index)
                .Where(index => arms.All(arm => arm.Fields.Any(field => field.Index == index)))
                .ToArray();

        private static TaggedEntryArm? ReadFlatTaggedEntryArm(
            Clause clause,
            int input,
            string? required = null,
            bool allowCarrierAlias = false
        )
        {
            Pattern shape = clause.Patterns[input];
            Pattern.Variable? carrier = null;
            if (allowCarrierAlias && shape is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var aliased))
            {
                carrier = binding;
                shape = aliased;
            }

            if (clause.Guard is not null || shape is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag))
            {
                return null;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (carrier is { Name: not VariableScopeNames.Wildcard })
            {
                names.Add(carrier.Name);
                inputs.Add(carrier.Name, input);
            }

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
